public static class XmpBuilder
{
    // 최소 필드만 넣은 심플 XMP. 필요시 태그 추가 가능(dc:title, keywords 등)
    public static string BuildSimpleXmp(string description, string createDateIso8601, string deviceModel, string latitude, string longitude)
    {
        // latitude/longitude가 비어있으면 태그 자체를 생략
        string gpsPart = string.Empty;
        if (!string.IsNullOrEmpty(latitude) && !string.IsNullOrEmpty(longitude))
        {
            gpsPart = $@"
    <exif:GPSLatitude>{latitude}</exif:GPSLatitude>
    <exif:GPSLongitude>{longitude}</exif:GPSLongitude>";
        }

        // XMP는 UTF-8 권장
        return $@"<?xpacket begin='﻿' id='W5M0MpCehiHzreSzNTczkc9d'?>
<x:xmpmeta xmlns:x='adobe:ns:meta/'>
  <rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'>
    <rdf:Description 
      xmlns:dc='http://purl.org/dc/elements/1.1/' 
      xmlns:xmp='http://ns.adobe.com/xap/1.0/' 
      xmlns:exif='http://ns.adobe.com/exif/1.0/' 
      xmlns:tiff='http://ns.adobe.com/tiff/1.0/'>
      <dc:description>
        <rdf:Alt>
          <rdf:li xml:lang='x-default'>{EscapeXml(description)}</rdf:li>
        </rdf:Alt>
      </dc:description>
      <xmp:CreateDate>{createDateIso8601}</xmp:CreateDate>
      <tiff:Model>{EscapeXml(deviceModel)}</tiff:Model>{gpsPart}
    </rdf:Description>
  </rdf:RDF>
</x:xmpmeta>
<?xpacket end='w'?>";
    }

    private static string EscapeXml(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
                .Replace("\"", "&quot;").Replace("'", "&apos;");
    }
}