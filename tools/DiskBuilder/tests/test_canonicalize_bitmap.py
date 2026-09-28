"""Bounded bitmap normalization checks on private generated format fixtures."""
from pathlib import Path
import hashlib
import sys
import tempfile
import unittest

sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from canonicalize_bitmap import BitmapCanonicalizationError, canonicalize_bitmap_padding
from verify_image import verify_image
from test_verify_image import checksum, fixture, put, word


def fill_padding(image, bitmaps):
    blocks=len(image)//512
    for i in range(4064):
        number=2+(len(bitmaps)-1)*4064+i
        if number>=blocks:
            field=1+i//32
            put(image,bitmaps[-1],field,word(image,bitmaps[-1],field)|(1<<(i%32)))
    checksum(image,bitmaps[-1],0)


class CanonicalizeBitmapTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.path=Path(self.temp.name)/'new-private.adf'
        self.image,self.files,self.nodes,self.bitmaps=fixture()

    def test_padding_and_checksum_only_then_independent_readback(self):
        canonical=bytes(self.image);fill_padding(self.image,self.bitmaps)
        self.path.write_bytes(self.image)
        result=canonicalize_bitmap_padding(self.path)
        self.assertEqual(result['status'],'canonicalized')
        self.assertEqual(result['cleared_padding_bits'],2306)
        self.assertEqual(result['changed_blocks'],self.bitmaps)
        self.assertEqual(result['before_sha256'],hashlib.sha256(self.image).hexdigest())
        self.assertEqual(self.path.read_bytes(),canonical)
        self.assertTrue(result['in_range_bits_preserved'])
        self.assertFalse(result['shipping_qualified']);self.assertFalse(result['pure_admitted'])
        self.assertEqual(verify_image(self.path,self.files,'Build')['status'],'passed')

    def test_canonical_image_is_not_rewritten(self):
        self.path.write_bytes(self.image);before=self.path.stat().st_mtime_ns
        result=canonicalize_bitmap_padding(self.path)
        self.assertEqual(result['status'],'already-canonical')
        self.assertEqual(result['changed_blocks'],[]);self.assertEqual(result['cleared_padding_bits'],0)
        self.assertEqual(result['before_sha256'],result['after_sha256'])
        self.assertEqual(self.path.stat().st_mtime_ns,before)

    def test_invalid_checksum_rejected_before_write(self):
        fill_padding(self.image,self.bitmaps);self.image[self.bitmaps[0]*512]^=1
        self.path.write_bytes(self.image)
        with self.assertRaisesRegex(BitmapCanonicalizationError,'checksum'):canonicalize_bitmap_padding(self.path)
        self.assertEqual(self.path.read_bytes(),self.image)

    def test_invalid_and_overlapping_bitmap_pointer_rejected_before_write(self):
        for pointer in (0x7fffffff,self.nodes['']):
            image=bytearray(self.image);put(image,self.nodes[''],79,pointer);checksum(image,self.nodes[''])
            self.path.write_bytes(image)
            with self.subTest(pointer=pointer),self.assertRaises(BitmapCanonicalizationError):canonicalize_bitmap_padding(self.path)
            self.assertEqual(self.path.read_bytes(),image)

    def test_bitmap_extension_and_final_map_only(self):
        self.image,self.files,self.nodes,self.bitmaps=fixture(106496)
        canonical=bytes(self.image);fill_padding(self.image,self.bitmaps);self.path.write_bytes(self.image)
        result=canonicalize_bitmap_padding(self.path)
        self.assertEqual(result['changed_blocks'],[self.bitmaps[-1]])
        self.assertEqual(len(result['bitmap_extension_blocks']),1)
        self.assertEqual(self.path.read_bytes(),canonical)
        self.assertEqual(verify_image(self.path,self.files,'Build')['status'],'passed')


if __name__=='__main__':unittest.main()
