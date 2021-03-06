# Fork with some new features.

## XML based serialization and deserialization

This version adds support for an alternative serialization format based on XML.
A sample MigraDoc/XML file can be found [here](https://raw.githubusercontent.com/emazv72/MigraDoc-samples/master/samples/xml/MigraDoc.xml) 

Example usage:

			MigraDoc.DocumentObjectModel.IO.Xml.DdlWriter.WriteToFile(document, "MigraDoc.xml");

			using (StreamReader sr = File.OpenText("MigraDoc.xml"))
			{
				var errors = new MigraDoc.DocumentObjectModel.IO.DdlReaderErrors();
				var reader = new MigraDoc.DocumentObjectModel.IO.Xml.DdlReader(sr, errors);

				document = reader.ReadDocument();

				using (StreamWriter sw = new StreamWriter("MigraDoc.xml.errors"))
				{
					foreach (MigraDoc.DocumentObjectModel.IO.DdlReaderError error in errors)
					{
						sw.WriteLine("{0}:{1} {2} {3}", error.SourceLine, error.SourceColumn, error.ErrorLevel, error.ErrorMessage);

					}

				}

			}
            
## Last page header and footer in a section

- Adds the DifferentLastPageHeaderFooter flag and the corresponding headers and footers:

        <Section>
            <Attributes>
                <PageSetup PageHeight="29.7cm" PageWidth="21cm" Orientation="Portrait" DifferentLastPageHeaderFooter="true"/>
            </Attributes>
            <LastPageHeader>
            ....
            </LastPageHeader>
            <LastPageFooter>
            ....
            </LastPageFooter>
        </Section>
		
## 1D & 2D barcode

- Barcode support via zxing:

        <Barcode Type="QRCode" Code="http://www.google.com" Width="3cm" Height="3cm" Resolution="200"/>

# MigraDoc
MigraDoc Foundation - Creating documents on the fly

MigraDoc references PDFsharp as a submodule. After pulling MigraDoc to a local repository, call
* git submodule init
* git submodule update

to update the submodule.

When forking MigraDoc, the fork will still reference the original PDFsharp repository. Consider forking PDFsharp, too, and use your fork as a submodule.

When downloading MigraDoc as a ZIP, the submodule PDFsharp will be empty. So also download a ZIP for the PDFsharp repository.

Please note: Source code is also available on SourceForge as a ZIP file. The MigraDoc ZIP file on SourceForge does include the PDFsharp files.

# Resources

The official project web site:  
http://pdfsharp.net/

The official peer-to-peer support forum:  
http://forum.pdfsharp.net/

# Release Notes for PDFsharp/MigraDoc 1.50 (stable)

The stable version of PDFsharp 1.32 was published in 2013.  
So a new stable version is long overdue.

I really hope the stable version does not have any regressions versus 1.50 beta 3b or later.

And I hope there are no regressions versus version 1.32 stable. But several bugs have been fixed.  
There are a few breaking changes that require code updates.

To use PDFsharp with Medium Trust you have to get the source code and make some changes. The NuGet packages do not support Medium Trust.  
Azure servers do not require Medium Trust.

I'm afraid that many users who never tried any beta version of PDFsharp 1.50 will now switch from version 1.32 stable to version 1.50 stable.  
Nothing wrong about that. I hope we don't get an avalanche of bug reports now.
