<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="2.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl">
    <xsl:output method="text" indent="no"/>

  <xsl:template match="/">
    <xsl:text> || Property name || Description ||
    </xsl:text>
    <xsl:apply-templates select="*"></xsl:apply-templates>
  </xsl:template>
  
  <xsl:template match="*[local-name()='element']">
  <xsl:text> | </xsl:text><xsl:value-of select="@name"/>
  <xsl:text> | </xsl:text>
  </xsl:template>
</xsl:stylesheet>
