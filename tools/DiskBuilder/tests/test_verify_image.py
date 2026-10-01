"""Reader tests build private format fixtures directly, without amitools.

No vendor assets or existing image are used. Independent writer integration is
tested by the DiskBuilder owner; these controls isolate reader failure modes.
"""
from pathlib import Path
import hashlib
import struct
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from verify_image import ImageVerificationError, verify_image


def checksum(image, block, field=5):
    struct.pack_into(">I", image, block * 512 + field * 4, 0)
    words = struct.unpack_from(">128I", image, block * 512)
    struct.pack_into(">I", image, block * 512 + field * 4, -sum(words) & 0xffffffff)


def put(image, block, field, value):
    struct.pack_into(">I", image, block * 512 + field * 4, value & 0xffffffff)


def word(image, block, field):
    return struct.unpack_from(">I", image, block * 512 + field * 4)[0]


def bucket(name):
    value = len(name)
    for char in name.upper().encode("ascii"):
        value = (13 * value + char) % 2048
    return value % 72


def fixture(blocks=1760):
    """Root/C/Docs, pure ordinary file, empty file, and a 73-block file."""
    image = bytearray(blocks * 512)
    image[:4] = b"DOS\1"
    root = blocks // 2
    bitmap_count = (blocks - 2 + 4063) // 4064
    bitmaps = list(range(root + 1, root + 1 + bitmap_count))
    bitmap_extensions = [root + 1 + bitmap_count] if bitmap_count > 25 else []
    used = {root, *bitmaps, *bitmap_extensions}
    next_block = 2
    def allocate():
        nonlocal next_block
        while next_block in used:
            next_block += 1
        result = next_block
        used.add(result)
        next_block += 1
        return result
    struct.pack_into(">I", image, 8, root)
    put(image, root, 0, 2); put(image, root, 3, 72); put(image, root, 127, 1)
    image[root * 512 + 432:root * 512 + 438] = b"\5Build"
    nodes = {"": root}
    for name in ("C", "Docs"):
        number = allocate(); nodes[name] = number
        put(image, number, 0, 2); put(image, number, 1, number)
        put(image, number, 125, root); put(image, number, 127, 2)
        image[number * 512 + 432:number * 512 + 433 + len(name)] = bytes([len(name)]) + name.encode()
    payloads = {"C/Tool": b"private-hunk-fixture\0", "C/Empty": b"", "Docs/ReadMe": bytes(range(256)) * 145 + b"tail"}
    files = []
    for path, payload in payloads.items():
        name = path.rsplit("/", 1)[1]; header = allocate(); nodes[path] = header
        blocks_for_data = [allocate() for _ in range((len(payload) + 511) // 512)]
        lists = [header] + [allocate() for _ in range((max(0, len(blocks_for_data) - 72) + 71) // 72)]
        for n, number in enumerate(lists):
            put(image, number, 0, 2 if n == 0 else 16); put(image, number, 1, number)
            table = blocks_for_data[n * 72:(n + 1) * 72]
            put(image, number, 2, len(table))
            for i, pointer in enumerate(table):put(image, number, 77 - i, pointer)
            put(image, number, 125, nodes[path.rsplit("/", 1)[0]] if n == 0 else header)
            put(image, number, 126, lists[n+1] if n+1 < len(lists) else 0)
            put(image, number, 127, -3)
            checksum(image, number)
        put(image, header, 4, blocks_for_data[0] if blocks_for_data else 0)
        put(image, header, 80, 32 if path == "C/Tool" else 0)
        put(image, header, 81, len(payload))
        image[header * 512 + 432:header * 512 + 433 + len(name)] = bytes([len(name)]) + name.encode()
        for i, number in enumerate(blocks_for_data):
            part = payload[i*512:(i+1)*512]; image[number*512:number*512+len(part)] = part
        checksum(image, header)
        files.append({"installed_path": path, "sha256": hashlib.sha256(payload).hexdigest(),
                      "bytes": len(payload), "amiga_protection": 32 if path == "C/Tool" else 0})
    for path, number in nodes.items():
        if not path: continue
        parent_path, name = path.rsplit("/",1) if "/" in path else ("",path)
        parent = nodes[parent_path]; slot = 6 + bucket(name)
        put(image, number, 124, word(image,parent,slot)); put(image,parent,slot,number)
        checksum(image,number)
    put(image, root, 78, -1)
    for n, number in enumerate(bitmaps[:25]):put(image,root,79+n,number)
    if bitmap_extensions:
        put(image,root,104,bitmap_extensions[0])
        for n, number in enumerate(bitmaps[25:]):put(image,bitmap_extensions[0],n,number)
    for index, number in enumerate(bitmaps):
        for n in range(4064):
            block = 2 + index*4064 + n
            if block < blocks and block not in used:
                field, mask = 1 + n // 32, 1 << (n % 32)
                put(image,number,field,word(image,number,field) | mask)
        checksum(image,number,0)
    for number in (root,nodes['C'],nodes['Docs']):checksum(image,number)
    return image, files, nodes, bitmaps


class VerifyImageTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.path = Path(self.temp.name)/"private.adf"
        self.image,self.files,self.nodes,self.bitmaps = fixture()

    def verify(self):
        self.path.write_bytes(self.image)
        return verify_image(self.path,self.files,"Build")

    def test_exact_tree_protection_empty_and_extended_file(self):
        result = self.verify()
        self.assertEqual(result['directories'],['C','Docs'])
        self.assertEqual(len(result['files']),3)
        self.assertEqual(next(x for x in result['files'] if x['installed_path']=='C/Tool')['amiga_protection'],32)
        self.assertEqual(len(next(x for x in result['files'] if x['installed_path']=='Docs/ReadMe')['extension_blocks']),1)
        self.assertTrue(result['bitmap']['exact_reachable_allocation'])
        self.assertFalse(result['shipping_qualified']); self.assertFalse(result['pure_admitted'])
        self.assertFalse(result['bootable'])

    def test_header_checksum_corruption(self):
        self.image[self.nodes['C/Tool']*512+320] ^= 32
        with self.assertRaisesRegex(ImageVerificationError,'checksum'):self.verify()

    def test_protection_change_with_valid_checksum(self):
        n=self.nodes['C/Tool'];put(self.image,n,80,0);checksum(self.image,n)
        with self.assertRaisesRegex(ImageVerificationError,'protection'):self.verify()

    def test_file_content_corruption(self):
        self.image[word(self.image,self.nodes['C/Tool'],4)*512] ^= 1
        with self.assertRaisesRegex(ImageVerificationError,'bytes/hash'):self.verify()

    def test_exact_spelling_is_required(self):
        n=self.nodes['C/Tool'];self.image[n*512+433]=ord('t');checksum(self.image,n)
        with self.assertRaisesRegex(ImageVerificationError,'membership or spelling'):self.verify()

    def test_missing_or_unexpected_tree_member(self):
        self.files.pop()
        with self.assertRaisesRegex(ImageVerificationError,'membership'):self.verify()

    def test_all_root_and_entry_dates_are_checked(self):
        for n,field in [(self.nodes[''],105),(self.nodes[''],118),(self.nodes[''],121),
                        (self.nodes['C'],105),(self.nodes['C/Tool'],105)]:
            with self.subTest(block=n,field=field):
                original=self.image[:];put(self.image,n,field,1);checksum(self.image,n)
                with self.assertRaisesRegex(ImageVerificationError,'epoch'):self.verify()
                self.image=original

    def test_wrong_hash_bucket_with_valid_checksums(self):
        n=self.nodes['C'];old=6+bucket('Tool');new=6+((bucket('Tool')+1)%72)
        self.assertEqual(word(self.image,n,new),0)
        put(self.image,n,new,word(self.image,n,old));put(self.image,n,old,0);checksum(self.image,n)
        with self.assertRaisesRegex(ImageVerificationError,'hash bucket'):self.verify()

    def test_used_block_marked_free_and_orphan_allocation(self):
        for block in (self.nodes['C/Tool'],1700):
            original=self.image[:];offset=block-2;field=1+offset//32;mask=1<<(offset%32)
            put(self.image,self.bitmaps[0],field,word(self.image,self.bitmaps[0],field)^mask)
            checksum(self.image,self.bitmaps[0],0)
            with self.subTest(block=block),self.assertRaisesRegex(ImageVerificationError,'Bitmap allocation'):self.verify()
            self.image=original

    def test_bitmap_checksum_and_outside_image_bits(self):
        self.image[self.bitmaps[0]*512+4]^=1
        with self.assertRaisesRegex(ImageVerificationError,'Bitmap checksum'):self.verify()
        checksum(self.image,self.bitmaps[0],0)
        # Remove the earlier edit, then mark a bit beyond this image free.
        self.image[self.bitmaps[0]*512+4]^=1
        put(self.image,self.bitmaps[0],127,0x80000000);checksum(self.image,self.bitmaps[0],0)
        with self.assertRaisesRegex(ImageVerificationError,'out-of-image'):self.verify()

    def test_cross_file_data_alias_is_rejected(self):
        # Give the empty file the first file's payload and hash, preserving
        # coherent headers/content so only cross-file ownership can reject it.
        header=self.nodes['C/Empty'];source=self.nodes['C/Tool'];pointer=word(self.image,source,4)
        put(self.image,header,2,1);put(self.image,header,4,pointer);put(self.image,header,77,pointer)
        put(self.image,header,81,self.files[0]['bytes']);checksum(self.image,header)
        self.files[1].update(bytes=self.files[0]['bytes'],sha256=self.files[0]['sha256'])
        with self.assertRaisesRegex(ImageVerificationError,'shared'):self.verify()

    def test_invalid_manifest_and_wrong_volume(self):
        for field,value in [('installed_path','../escape'),('amiga_protection',True),('sha256','x'*64),('bytes',-1)]:
            old=self.files[0][field];self.files[0][field]=value
            with self.subTest(field=field),self.assertRaises(ImageVerificationError):self.verify()
            self.files[0][field]=old
        self.path.write_bytes(self.image)
        with self.assertRaisesRegex(ImageVerificationError,'Volume'):verify_image(self.path,self.files,'Other')

    def test_boot_code_and_other_dos_variants_rejected(self):
        self.image[12]=1
        with self.assertRaisesRegex(ImageVerificationError,'boot code'):self.verify()
        self.image[12]=0;self.image[3]=3
        with self.assertRaisesRegex(ImageVerificationError,'DOS1'):self.verify()

    def test_raw_hdf_with_bitmap_extension(self):
        self.image,self.files,self.nodes,self.bitmaps=fixture(106496) #52 MiB,26 maps
        self.path=self.path.with_suffix('.hdf');result=self.verify()
        self.assertEqual(len(result['bitmap']['blocks']),27)
        self.assertEqual(len(result['bitmap']['extension_blocks']),1)

    def test_image_size_ceiling_before_read(self):
        with self.path.open('wb') as stream:stream.truncate(512*1024*1024+512)
        with self.assertRaisesRegex(ImageVerificationError,'512 MiB'):verify_image(self.path,self.files,'Build')


if __name__ == '__main__':unittest.main()
