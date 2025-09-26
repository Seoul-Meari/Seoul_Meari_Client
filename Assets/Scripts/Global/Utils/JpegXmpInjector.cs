using System;
using System.Text;

public static class JpegXmpInjector
{
    // APP1 XMP 헤더 시그니처
    private static readonly byte[] XmpHeader = Encoding.ASCII.GetBytes("http://ns.adobe.com/xap/1.0/\0");

    public static byte[] InjectXmp(byte[] jpegBytes, string xmpPacket)
    {
        if (jpegBytes == null || jpegBytes.Length < 4)
            throw new ArgumentException("Invalid JPEG data");

        // JPEG SOI 체크 (0xFF, 0xD8)
        if (!(jpegBytes[0] == 0xFF && jpegBytes[1] == 0xD8))
            throw new ArgumentException("Not a JPEG file (missing SOI)");

        byte[] xmpData = Encoding.UTF8.GetBytes(xmpPacket);
        // APP1(0xFFE1) + length(2 bytes BE) + XmpHeader + xmpData
        int contentLen = XmpHeader.Length + xmpData.Length;
        int app1Len = contentLen + 2; // length field includes itself

        byte[] app1 = new byte[2 + 2 + contentLen];
        app1[0] = 0xFF; app1[1] = 0xE1; // APP1 marker
        app1[2] = (byte)((app1Len >> 8) & 0xFF);
        app1[3] = (byte)(app1Len & 0xFF);
        Buffer.BlockCopy(XmpHeader, 0, app1, 4, XmpHeader.Length);
        Buffer.BlockCopy(xmpData, 0, app1, 4 + XmpHeader.Length, xmpData.Length);

        // 간단히 SOI 바로 뒤에 삽입 (기존 APPn 앞에 둘 수도 있음)
        byte[] output = new byte[jpegBytes.Length + app1.Length];
        // SOI
        output[0] = 0xFF; output[1] = 0xD8;
        // APP1
        Buffer.BlockCopy(app1, 0, output, 2, app1.Length);
        // 나머지 원본 (SOI 이후)
        Buffer.BlockCopy(jpegBytes, 2, output, 2 + app1.Length, jpegBytes.Length - 2);
        return output;
    }
}