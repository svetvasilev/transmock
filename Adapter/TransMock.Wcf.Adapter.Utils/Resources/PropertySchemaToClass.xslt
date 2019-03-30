<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="2.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
>
    <xsl:output method="text" indent="yes"/>

  <xsl:template match="/">
    <xsl:variable name="Namespace" select="/*[local-name()='schema']/@targetNamespace"/>
    <xsl:variable name="NamePrefix" select="substring-after(substring-before($Namespace,'-properties'),'http://schemas.microsoft.com/BizTalk/2003/')"></xsl:variable>
    <xsl:text>
    using System;
    using System.Linq;

    namespace TransMock.Utils
    {

        public partial class BizTalkProperties
        {
            public class  
    </xsl:text><xsl:value-of select="$NamePrefix" />
            <xsl:text>
            {
            </xsl:text>
            <xsl:apply-templates select="//*[local-name()='element']" />

            <xsl:text>
            }
        }
    }
    </xsl:text>
  </xsl:template>

  <xsl:template match="*[local-name()='element']">
    <xsl:variable name="Namespace" select="/*[local-name()='schema']/@targetNamespace"/>
    <xsl:variable name="NamePrefix" select="substring-after(substring-before($Namespace,'-properties'),'http://schemas.microsoft.com/BizTalk/2003/')"></xsl:variable>
          <xsl:text> 
                public string </xsl:text><xsl:value-of select="@name" />
          <xsl:text>
                {
                    get
                    {
                        return this.propertyExtractor.GetProperty("</xsl:text><xsl:value-of select="concat($NamePrefix,'_', @name)"/><xsl:text>");
                    }
                }

          </xsl:text>    
  </xsl:template>
</xsl:stylesheet>
