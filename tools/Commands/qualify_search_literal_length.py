"""Measure and qualify Search literal-length hoisting with frozen compiler inputs."""
import argparse
from concurrent.futures import ThreadPoolExecutor
import json
from pathlib import Path
import shutil
import os
import sys
from measure_c import identity, git_state, parse_map
from qualify_sizes import run, qualify, comparable
from investigate_list_search_loops import build

REPO=Path(__file__).resolve().parents[2]
PREVIOUS=REPO/'artifacts/list-search-loops-20261018'


def read(path):return json.loads(path.read_text(encoding='utf-8-sig'))
def save(path,value):path.write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')


def prepare(stage):
    assert not stage.exists()
    stage.mkdir(parents=True)
    for source,target in [('compiler','compiler'),('runner-final','runner'),('dependencies','dependencies'),('inputs/Search','inputs/Search')]:
        shutil.copytree(PREVIOUS/source,stage/target)
    shutil.copytree(REPO/'out/C',stage/'installed/C')
    shutil.copytree(REPO/'src/Commands/Search',stage/'baseline-source',ignore=shutil.ignore_patterns('bin','obj'))
    save(stage/'freeze.json',dict(source=git_state(REPO),compiler='9eaa2aa',
        previous=identity(REPO/'docs/Commands/Workbench31MorphOS320/list-search-loops-20261018.json'),
        privateEntry=identity(REPO/'tests/Commands.AddBuffersNativeRoot/NativeMorphOSSearchEntry.cs'),
        files=[identity(p) for p in sorted(stage.rglob('*')) if p.is_file()]))


def variants(stage):
    for label in ['line','forward','cache','accepted','cached-index','index-only']:
        source=stage/(label+'-source');assert not source.exists();shutil.copytree(stage/'baseline-source',source)
        path=source/'Native/NativeMorphOSSearchCommand.cs';text=path.read_text(encoding='utf-8-sig')
        if label in ['cache','accepted','cached-index']:
            old='''                APTR.WriteUInt8(pattern, unchecked((int)searchLength), 0);
            }
'''
            assert text.count(old)==1
            text=text.replace(old,'''                APTR.WriteUInt8(pattern, unchecked((int)searchLength), 0);
                // Literal mode no longer needs the pattern compiler's source
                // scratch. Keep its immutable length in that invocation-owned slot.
                APTR.WriteUInt32(userPattern, 0, searchLength);
            }
''')
            text=text.replace('var patternLength = CStringLength(pattern, PatternBytes - 1);',
                '''// Literal preparation reuses the dead compiler-source scratch
        // for this length. FILE/PATTERN never enter the literal matcher.
        var patternLength = APTR.ReadUInt32(pattern, unchecked((int)PatternBytes));''')
            if label in ['accepted','cached-index']:
                text=text.replace('    private const uint UserPatternBytes = 1024;',
                    '    private const uint UserPatternBytes = 1024;\n    private const uint LiteralPatternLengthOffset = PatternBytes;')
                text=text.replace('APTR.ReadUInt32(pattern, unchecked((int)PatternBytes));',
                    'APTR.ReadUInt32(pattern, unchecked((int)LiteralPatternLengthOffset));')
        elif label!='index-only':
            text=text.replace('APTR text, uint length, APTR pattern,\n        bool caseSensitive',
                              'APTR text, uint length, APTR pattern,\n        uint patternLength, bool caseSensitive')
            text=text.replace('        var patternLength = CStringLength(pattern, PatternBytes - 1);\n','')
            text=text.replace('LiteralContains(line, length, pattern, caseSensitive,',
                              'LiteralContains(line, length, pattern, patternLength, caseSensitive,')
            if label=='line':
                for signature in ['private static bool FormatLines(', 'private static bool FormatMorphosChunk(']:
                    start=text.index(signature);at=text.index('        var offset = 0u;',start)
                    text=text[:at]+'''        var patternLength = patternMode ? 0u : CStringLength(pattern, PatternBytes - 1);
'''+text[at:]
            else:
                text=text.replace('uint sourceLength, APTR pattern, bool useLocale,',
                                  'uint sourceLength, APTR pattern, uint patternLength, bool useLocale,')
                text=text.replace('uint capacity, APTR source, uint sourceLength, APTR pattern,\n        uint locale,',
                                  'uint capacity, APTR source, uint sourceLength, APTR pattern,\n        uint patternLength, uint locale,')
                text=text.replace('APTR destination, APTR pattern, uint locale, bool patternMode,',
                                  'APTR destination, APTR pattern, uint patternLength, uint locale, bool patternMode,')
                text=text.replace('outputBuffer, pattern, locale,','outputBuffer, pattern, searchLength, locale,')
                text=text.replace('unchecked((uint)bytes), pattern, false,','unchecked((uint)bytes), pattern, searchLength, false,')
                text=text.replace('buffer, sourceLength, pattern, locale, patternMode,','buffer, sourceLength, pattern, patternLength, locale, patternMode,')
        if label in ['cached-index','index-only']:
            start=text.index('    private static bool LiteralContains(');end=text.index('    private static byte Fold(',start)
            body=text[start:end].replace('            var matched = true;\n            for (var index = 0u; index < patternLength; index++)',
                '            var index = 0u;\n            for (; index < patternLength; index++)')
            body=body.replace('                matched = false;\n','').replace('            if (matched) return true;',
                '            if (index == patternLength) return true;')
            text=text[:start]+body+text[end:]
        path.write_text(text,encoding='utf-8')


def compile_variant(stage,label,sources):
    with ThreadPoolExecutor(max_workers=2) as pool:
        jobs=[pool.submit(build,stage,'Search',label,sources,profile) for profile in ['shipping','morphos']]
        for job in jobs:job.result()


def vectors(stage,label,expanded=False):
    for profile in ['shipping','morphos']:
        suite='wb31-search-native-entry-vector-fixture' if profile=='shipping' else 'morphos320-search-native-entry-vector-fixture'
        before,after=stage/'baseline'/profile,stage/label/profile
        if expanded:
            folder='expanded' if label=='accepted' else 'expanded-'+label
            for version,source in [('baseline',before),('candidate',after)]:
                dest=stage/folder/profile/version;assert not dest.exists();shutil.copytree(source/'C',dest/'C')
            before,after=stage/folder/profile/'baseline',stage/folder/profile/'candidate'
        runner=stage/('runner-expanded' if expanded else 'runner')/'CopperOS.Commands.NativeExecution.dll'
        rows=qualify(before,after,runner,
                     ['Search'],{'Search':[suite]})
        assert rows[0]['qualified']


def native(stage,label):
    from qualify_list_search_fixed import verifier
    folder='native' if label=='accepted' else 'native-'+label
    root=stage/folder/'Search';(root/'inputs').mkdir(parents=True)
    for folder in ['host','guest','runner']:shutil.copytree(PREVIOUS/'native/Search'/folder,root/folder)
    for file in ['TEXT.IN','LONG.IN']:shutil.copy2(PREVIOUS/'native/Search/inputs'/file,root/'inputs'/file)
    for version,source in [('baseline','baseline'),('candidate',label)]:
        shutil.copy2(stage/source/'shipping/C/Search',root/'inputs'/('Search.'+version))
    rom=Path('C:/Data/ROM/kickstart-3.1-a500.rom');save(stage/'rom.json',identity(rom))
    assert identity(rom)['sha256']=='8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee'
    host=root/'host/CopperOS.SearchQualification.Host.dll';tools=REPO/'tools/Commands/SearchOsQualification'
    assert run(['dotnet',str(host),'--bind',str(root/'runner'),str(root/'runner-source-binding.json')],root/'bind.log')==0
    assert run([sys.executable,str(tools/'cases.py'),str(root)],root/'cases.log')==0
    cases=read(root/'cases.json');assert len(cases)==60
    save(root/'cases-part1.json',cases[:30]);save(root/'cases-part2.json',cases[:4]+cases[30:])
    def one(job):
        cpu,batch=job
        assert run(['dotnet',str(host),str(root),str(rom),cpu,str(root/('cases-'+batch+'.json')),batch],root/(batch+'-'+cpu+'.log'))==0
        result=verifier('Search')(root,batch,cpu);assert result['passed'],result['failures']
        return result
    with ThreadPoolExecutor(max_workers=2) as pool:rows=list(pool.map(one,[(cpu,b) for cpu in ['68000','68020','68040'] for b in ['part1','part2']]))
    save(stage/(folder+'.json'),rows)
    assert run([sys.executable,str(tools/'control-auditor.py'),str(root)],root/'controls.log')==0
    os.environ['COPPERSHARP_KICKSTART31_ROM']=str(rom)
    assert run(['pwsh','-NoProfile','-File',str(tools/'confirm-winuae.ps1'),'-Stage',str(root),'-RequireCorrectOutput'],root/'winuae.log')==0


def audit(stage,label):
    from qualify_list_search_fixed import verifier
    frozen=read(stage/'freeze.json')
    for item in frozen['files']:assert identity(item['path'])==item
    assert identity(frozen['previous']['path'])==frozen['previous']
    assert identity(frozen['privateEntry']['path'])==frozen['privateEntry']
    previous=read(Path(frozen['previous']['path']))
    for item in previous['freeze']['files']:
        file=Path(item['path'])
        if file.is_relative_to(PREVIOUS/'compiler'):
            current=identity(stage/'compiler'/file.relative_to(PREVIOUS/'compiler'))
            assert (current['bytes'],current['sha256'])==(item['bytes'],item['sha256'])
    assert (stage/'baseline/shipping/C/Search').read_bytes()==(stage/'installed/C/Search').read_bytes()
    for profile in ['shipping','morphos']:
        assert (stage/'baseline'/profile/'C/Search').read_bytes()==(PREVIOUS/'search-combined'/profile/'C/Search').read_bytes()
    rows=[];trials=[];maps={}
    expanded='expanded' if label=='accepted' else 'expanded-'+label
    native_folder='native' if label=='accepted' else 'native-'+label
    for profile in ['shipping','morphos']:
        root=stage/expanded/profile;check=read(root/'candidate/qualification.json')[0]
        assert check['accept'] and check['qualified']
        assert {c['cpu'] for c in check['checks']}=={'68000','68020','68040'}
        changes=[]
        for cpu in ['68000','68020','68040']:
            observations={}
            for version,source in [('baseline','baseline'),('candidate',label)]:
                image=root/version/'C/Search'
                assert image.read_bytes()==(stage/source/profile/'C/Search').read_bytes()
                suite='wb31-search-native-entry-vector-fixture' if profile=='shipping' else 'morphos320-search-native-entry-vector-fixture'
                receipt=read(root/version/'execution'/('Search.'+cpu+'.'+suite+'.json'))
                assert receipt['status']=='passed' and len(receipt['cases'])==(30 if profile=='shipping' else 57)
                assert receipt['imageSha256']==identity(image)['sha256']
                assert receipt['managedExecutorSha256']==identity(stage/'runner-expanded/CopperOS.Commands.NativeExecution.dll')['sha256']
                assert receipt['instructionCoreSha256']=='99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f'
                assert all(c['peakStackBytes']<=c.get('configuredStackBytes',8192) for c in receipt['cases'])
                observations[version]=receipt['cases']
            assert [comparable(c) for c in observations['baseline']]==[comparable(c) for c in observations['candidate']]
            changes += [dict(cpu=cpu,name=a['name'],instructions=b['instructions']-a['instructions']) for a,b in zip(observations['baseline'],observations['candidate'])]
        assert all(c['instructionsAfter']<c['instructionsBefore'] and c['peakStackAfter']<=c['peakStackBefore'] for c in check['checks'])
        rows.append(dict(profile=profile,qualification=check,instructionDeltas=changes))
        for trial in ['baseline','line','forward','cache','accepted','index-only','cached-index']:
            image=stage/trial/profile/'C/Search';m=parse_map(Path(str(image)+'.map'))
            trials.append(dict(profile=profile,label=trial,image=identity(image),response=identity(stage/trial/profile/'Search.rsp')))
            maps[trial+'/'+profile]=dict(metrics=m['records']['METRICS'],effectivePasses=m['records'].get('GENERATED-SIZE'),
                rewrites=m['records'].get('GENERATED-SIZE-REWRITE'),reachableMethods=m['reachableMethods'],largestMethods=m['largestMethods'])
        assert (stage/('source-proof-'+label)/profile/'C/Search').read_bytes()==(stage/label/profile/'C/Search').read_bytes()
    root=stage/native_folder/'Search';native=[]
    for cpu in ['68000','68020','68040']:
        for batch in ['part1','part2']:
            result=verifier('Search')(root,batch,cpu);assert result['passed'],result['failures']
            assert result['result']['RomSha256'].lower()==read(stage/'rom.json')['sha256']
            native.append(result)
    assert sum(r['invocations'] for r in native)==192
    for version,source in [('baseline','baseline'),('candidate',label)]:
        assert (root/'inputs'/('Search.'+version)).read_bytes()==(stage/source/'shipping/C/Search').read_bytes()
    controls=read(root/'negative-controls.json');assert len(controls)==4 and all(c['rejected'] for c in controls)
    binding=read(root/'runner-source-binding.json');assert all(d['matches'] for r in binding[:2] for d in r['documents'])
    winuae=read(root/'winuae/results.json');assert {r['cpu'] for r in winuae}=={'68000','68020','68040'}
    for result in winuae:
        assert result['completed'] and result['outputsAgree'] and result['numberedOutputCorrect']
        assert not result['jit'] and result['stackBytes']==4096
        assert result['romSha256']==read(stage/'rom.json')['sha256']
        assert result['baseline']==result['candidate']=='     2> Needle\n' and result['returns']=='baseline 0 0\ncandidate 0 0\n'
        sysroot=root/'winuae'/result['cpu']/'sys'
        for version,suffix in [('baseline','B'),('candidate','C')]:assert (sysroot/'C'/('Search'+suffix)).read_bytes()==(root/'inputs'/('Search.'+version)).read_bytes()
        for key in ['baseline','candidate','returns']:assert (sysroot/(key+'.txt')).read_bytes().decode('latin1')==result[key]
    snapshot=list((stage/'runner-source/source').glob('*-SearchEntrySuite.cs'))
    assert len(snapshot)==1 and snapshot[0].read_bytes()==(REPO/'tests/Commands.NativeExecution/SearchEntrySuite.cs').read_bytes()
    additional=[]
    for trial,folder in [('cache-only','expanded'),('index-only','expanded-index-only')]:
        for profile in ['shipping','morphos']:
            result=read(stage/folder/profile/'candidate/qualification.json')[0]
            assert result['qualified'] and {c['cpu'] for c in result['checks']}=={'68000','68020','68040'}
            additional.append(dict(trial=trial,profile=profile,qualification=result))
    method_details=read(stage/'literal-method-details.json')
    for item in method_details:assert identity(item['disassembly']['path'])==item['disassembly']
    data=dict(passed=True,selected=label,freeze=frozen,trials=trials,maps=maps,vectors=rows,native=native,negativeControls=controls,winuae=winuae,
        additionalQualification=additional,methodDetails=method_details,
        sources=[identity(p) for p in sorted((stage/(label+'-source')).rglob('*.cs'))],
        fixtureSources=[identity(p) for p in sorted((stage/'runner-source/source').glob('*.cs'))],
        tool=identity(Path(__file__)),buildTool=identity(REPO/'tools/Commands/investigate_list_search_loops.py'),
        runner=identity(stage/'runner-expanded/CopperOS.Commands.NativeExecution.dll'),
        runtime=[identity(p) for directory in ['runner','host','guest'] for p in sorted((root/directory).rglob('*')) if p.is_file()],
        buildReceipts=[identity(p) for p in sorted(stage.glob('*-build.log'))]+[identity(p) for p in sorted(stage.glob('*/Build.csproj'))],
        evidence=[identity(p) for directory in [expanded,native_folder,'expanded','native','expanded-index-only'] for p in sorted((stage/directory).rglob('*.json'))])
    if (stage/'installed-inventory.json').exists():
        inventory=read(stage/'installed-inventory.json');assert len(inventory)==61
        for item in inventory:
            accepted=identity(stage/'accepted-inventory/C'/Path(item['path']).name)
            assert (accepted['bytes'],accepted['sha256'])==(item['bytes'],item['sha256'])
        data.update(installedInventory=inventory,totalBefore=sum(p.stat().st_size for p in (stage/'installed/C').iterdir()),totalAfter=sum(p['bytes'] for p in inventory))
    save(stage/'audit.json',data);return data


def publish(stage,label):
    audit(stage,label)
    assert len(list((stage/'installed/C').iterdir()))==61
    for file in (stage/'installed/C').iterdir():assert (REPO/'out/C'/file.name).read_bytes()==file.read_bytes()
    for file in (stage/(label+'-source')).rglob('*.cs'):
        assert (REPO/'src/Commands/Search'/file.relative_to(stage/(label+'-source'))).read_bytes()==file.read_bytes()
    accepted=stage/'accepted-inventory/C';assert not accepted.exists();shutil.copytree(stage/'installed/C',accepted)
    shutil.copy2(stage/label/'shipping/C/Search',accepted/'Search')
    assert (accepted/'Search').stat().st_size<(stage/'installed/C/Search').stat().st_size
    shutil.copy2(accepted/'Search',REPO/'out/C/Search')
    assert all((REPO/'out/C'/file.name).read_bytes()==file.read_bytes() for file in accepted.iterdir())
    save(stage/'installed-inventory.json',[identity(p) for p in sorted((REPO/'out/C').iterdir())])
    save(REPO/'docs/Commands/Workbench31MorphOS320/search-literal-length-20261018.json',audit(stage,label))


if __name__=='__main__':
    parser=argparse.ArgumentParser(__doc__);parser.add_argument('mode',choices=['prepare','variants','build','vectors','native','audit','publish'])
    parser.add_argument('--stage',type=Path,required=True);parser.add_argument('--label');parser.add_argument('--sources',type=Path);parser.add_argument('--expanded',action='store_true');args=parser.parse_args();stage=args.stage.resolve()
    if args.mode=='prepare':prepare(stage)
    elif args.mode=='variants':variants(stage)
    elif args.mode=='build':compile_variant(stage,args.label,args.sources.resolve() if args.sources else stage/(args.label+'-source'))
    elif args.mode=='native':native(stage,args.label)
    elif args.mode=='audit':audit(stage,args.label)
    elif args.mode=='publish':publish(stage,args.label)
    else:vectors(stage,args.label,args.expanded)
