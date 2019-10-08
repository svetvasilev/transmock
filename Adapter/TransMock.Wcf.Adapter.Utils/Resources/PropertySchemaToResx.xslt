<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="2.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
>
    <xsl:output method="xml" indent="yes"/>

    <xsl:template match="*[local-name()='element']">
      <xsl:variable name="Namespace" select="/*[local-name()='schema']/@targetNamespace"/>
      <xsl:variable name="NamePrefix" select="substring-after(substring-before($Namespace,'-properties'),'http://schemas.microsoft.com/BizTalk/2003/')"></xsl:variable>
      <xsl:element name="data">
        <xsl:attribute name="name">
          <xsl:value-of select="concat($NamePrefix,'_', @name)"/>
        </xsl:attribute>
        <xsl:element name="value">
          <xsl:value-of select="concat($Namespace,'#',@name)"/>
        </xsl:element>
      </xsl:element>
    </xsl:template>
</xsl:stylesheet>
