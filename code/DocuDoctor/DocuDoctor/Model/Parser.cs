using System.IO;
using System.Text.RegularExpressions;

namespace DocuDoctor.Model {


    /// <summary>
    /// Reads a file and outputs all read Classes
    /// </summary>
    public class Parser {
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: Parser : Parser                                       ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Riley Horling                                         ::
        :: 3. Created: 1/31/2025                                            ::
        :: 4. Purpose: Initiliases the Parser                               ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: fileName - path to the file to be read      ::
        :: 6. Output Parameters: bool, did the file contain any classes     ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        private string file;
        private string[] keywords;


        public Parser(String fileName) { //comment
            file = fileName;
        }

        private string readFile() {
            string fileContent = "";
            try {

                //Initialize 
                Regex commentAndString = new Regex("\\/\\*[\\S\\s]*\\*\\/|\"[\\S\\s]*\"|'[\\S\\s]*'");
                Regex whiteSpace = new Regex("[ \t\n\r]+");
                using(StreamReader sr = new StreamReader(file)) {
                    while(!sr.EndOfStream) {
                        string line = sr.ReadLine();

                        //Sees if ther are any comments in the line and cuts them out
                        int cutoff = line.IndexOf("//");
                        if(cutoff >= 0)
                            line = line.Remove(cutoff);
                        fileContent += line;
                    }
                }

                fileContent = commentAndString.Replace(fileContent, " ");
                fileContent = whiteSpace.Replace(fileContent, " ");
            } catch(Exception ex) {
                if(ex is FileNotFoundException) {
                    throw new FileNotFoundException("Invalid Input File");
                }
            }

            return fileContent;
        }


        public List<UmlBox> ParseFile() {
            string fileContent = readFile();
            //Scans for keywords like class and private and takes everything up to the line ender
            Regex findKeywords = new Regex("(private|public|class|protected).*[);}]");
            MatchCollection keywordChunks = findKeywords.Matches(fileContent);
            List<UmlBox> result = new List<UmlBox>();
            foreach(Match match in keywordChunks) {
                string chunk = match.Value;
                if(chunk.Contains('=') || (chunk.Contains(';') && !chunk.Contains(')'))) {
                    UmlVariable temp = readVariable(chunk);
                    if(temp != null && result[result.Count - 1] != null) {
                        result[result.Count - 1].AddVariable(temp);
                    }
                }
            }
            return result;
        }

        private UmlVariable readVariable(string text) {
            UmlVariable variable = null;
            string[] chunks = text.Split(' ');
            if(chunks.Length >= 3)
                variable = new UmlVariable(chunks[0], chunks[1], chunks[2]);
            return variable;
        }

        private UmlMethod readMethod() {
            return null;
        }

        private UmlBox readClass() {

            return null;
        }
    }
}
