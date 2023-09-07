# Frends.Kungsbacka.Zip

Tasks for managing zip files.



| Task          | Description                                         | Returns               |
| ------------- | --------------------------------------------------- | --------------------- |
| UnpackZipFile | Unpacks .zip and .7z files to a specified location. | bool denoting success |
| ExtractFilesBySearchString | Looks through an archive and extracts all files matching the search string. | bool denoting success, <br> List\<string\> of extracted filenames |
| FindAllZipArchivesInFolder | Locates all .zip and .7z archives within a folder | List\<string> of the names of found archives |
