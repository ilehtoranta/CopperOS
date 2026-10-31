import datetime
import copy
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
COMMANDS = ROOT / 'src' / 'Commands'
LEDGER = COMMANDS / 'CommandVersions.props'


class CommandVersionsTests(unittest.TestCase):
    def test_ledger_covers_projects_once_with_valid_dates(self):
        projects = []
        for path in COMMANDS.glob('*/*.csproj'):
            projects.extend(e.text.strip() for e in ET.parse(path).iter('CopperOSCommandName') if e.text and e.text.strip())
        entries = list(ET.parse(LEDGER).iter('_CopperOSCommandVersion'))
        names = [entry.attrib['Include'] for entry in entries]
        self.assertEqual(len(names), len(set(name.casefold() for name in names)))
        self.assertCountEqual(names, projects)
        for entry in entries:
            self.assertRegex(entry.findtext('Version'), r'^\d+\.\d+$')
            datetime.datetime.strptime(entry.findtext('Date'), '%d.%m.%Y')
            self.assertIn("'$(CopperOSCommandName)' == '" + entry.attrib['Include'] + "'", entry.attrib['Condition'])

    def evaluate(self, name, ledger, *args):
        project = COMMANDS / name / f'CopperOS.Commands.{name}.csproj'
        return subprocess.run(['dotnet', 'msbuild', str(project), '-nologo',
            '-t:CopperOSValidateCommandVersion', '-getProperty:CopperOSBinaryVersion,CopperOSBinaryDate',
            '-p:CopperOSCommandVersionsFile=' + str(ledger), *args], text=True, capture_output=True)

    def test_independent_records_overrides_and_missing_record(self):
        tree = ET.parse(LEDGER)
        entry = next(e for e in tree.iter('_CopperOSCommandVersion') if e.attrib['Include'] == 'Beep')
        reboot = next(e for e in tree.iter('_CopperOSCommandVersion') if e.attrib['Include'] == 'Reboot')
        reboot_version, reboot_date = reboot.findtext('Version'), reboot.findtext('Date')
        entry.find('Version').text = '2.7'
        entry.find('Date').text = '1.1.2027'
        with tempfile.TemporaryDirectory() as directory:
            ledger = Path(directory) / 'versions.props'
            tree.write(ledger, encoding='utf-8', xml_declaration=True)
            for name, version, date in [('Beep', '2.7', '1.1.2027'), ('Reboot', reboot_version, reboot_date)]:
                result = self.evaluate(name, ledger)
                self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                props = json.loads(result.stdout)['Properties']
                self.assertEqual((props['CopperOSBinaryVersion'], props['CopperOSBinaryDate']), (version, date))
            result = self.evaluate('Beep', ledger, '-p:CopperOSBinaryVersion=3.2', '-p:CopperOSBinaryDate=2.2.2027')
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertEqual(json.loads(result.stdout)['Properties']['CopperOSBinaryVersion'], '3.2')
            entry.attrib['Condition'] = "'$(CopperOSCommandName)' == 'Other'"
            tree.write(ledger, encoding='utf-8', xml_declaration=True)
            result = self.evaluate('Beep', ledger)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn('Expected exactly one version record', result.stdout + result.stderr)
            entry.attrib['Condition'] = "'$(CopperOSCommandName)' == 'Beep'"
            tree.getroot().find('ItemGroup').append(copy.deepcopy(entry))
            tree.write(ledger, encoding='utf-8', xml_declaration=True)
            result = self.evaluate('Beep', ledger)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn('Expected exactly one version record', result.stdout + result.stderr)


if __name__ == '__main__':
    unittest.main()
