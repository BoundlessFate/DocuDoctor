using System.IO;
using System.Text.RegularExpressions;

namespace DocuDoctor.Model {


    /// <summary>
    /// Reads a file and outputs all read Classes
    /// </summary>
    public class Parser {

        private string file;
        private string[] keywords;

        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: Parser : Parser                                       ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Riley Horling                                         ::
        :: 3. Created: 1/31/2025                                            ::
        :: 4. Purpose: Initiliases the Parser                               ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: fileName - path to the file to be read      ::
        :: 6. Preconditions: None                                           ::
        :: 7. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 8. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        public Parser(String fileName) { //comment
            file = fileName;
        }


        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: Parser : readFile                                       ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Riley Horling                                         ::
        :: 3. Created: 2/10/2025                                            ::
        :: 4. Purpose: Cleans up the file into a single line with comments  ::
        :: and strings removed                                              ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters:                                             ::
        :: 6. Output Parameters: The text of the inputted document with     ::
        :: comments. strings and newlines removed                           ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: FileNotFound                                          ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
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
                if(ex is FileNotFoundException || ex is NullReferenceException) {
                    throw new FileNotFoundException("Invalid Input File");
                }
            }

            return fileContent;
        }


        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: Parser : ParseFile                                       ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Riley Horling                                         ::
        :: 3. Created: 2/10/2025                                            ::
        :: 4. Purpose: Reads the file and scans for valid UML boxes::
        :: ---------------------------------------------------------------- ::
        :: 6. Output Parameters: List of found uml boxes ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/

        public List<UmlBox> ParseFile() {
            string fileContent = readFile();
            //Scans for keywords like class and private and takes everything up to the line ender
            Regex findKeywords = new Regex("(private |public |class |protected ).*?[);{]");
            MatchCollection keywordChunks = findKeywords.Matches(fileContent);
            List<UmlBox> result = new List<UmlBox>();
            foreach(Match match in keywordChunks) {
                string chunk = match.Value;
                if(chunk.Contains('=') || (chunk.Contains(';') && !chunk.Contains(')'))) {
                    UmlVariable temp = readVariable(chunk);
                    if(temp != null && result[result.Count - 1] != null) {
                        result[result.Count - 1].AddVariable(temp);
                    }
                } else if(chunk.Contains("class ")) {
                    result.Add(readClass(chunk));
                } else {
                    UmlMethod method = readMethod(chunk);
                    if(method != null && result[result.Count - 1] != null) {
                        result[result.Count - 1].AddMethod(method);
                    }
                }
            }
            return result;
        }



        private UmlVariable readVariable(string text) {
            UmlVariable variable = null;
            int removeIndex = Math.Max(text.IndexOf('='), text.IndexOf(';'));
            if(removeIndex > 0)
                text = text.Remove(removeIndex);
            string[] chunks = text.Split(' ');
            //TODO: change this into a something better
            if(chunks.Length >= 3)
                variable = new UmlVariable(chunks[0], chunks[1], chunks[2]);
            return variable;
        }

        private UmlMethod readMethod(string chunk) {
            // Seperate all the parameters for the method (assumes we use , for seperation)
            string[] parameters = Regex.Match(chunk, "(?<=\\().*?(?=\\))").Value.Split(",");

            //Gets rid of the parmeters from the method text
            chunk = Regex.Replace(chunk, "\\(.*?\\)", " ");
            //breaks the method into protection return type and name
            string[] keywords = chunk.Split(" ");

            UmlMethod method = null;
            List<UmlVariable> inputParam = new List<UmlVariable>();
            foreach(string param in parameters) {
                if(param.Length > 0) {

                    int splitPoint = param.LastIndexOf(" ");
                    if(splitPoint > 0) {
                        string type = param.Substring(0, splitPoint);
                        string name = param.Substring(splitPoint + 1);
                        inputParam.Add(new UmlVariable("", type, name));
                    } else {
                        inputParam.Add(new UmlVariable("", "", param));
                    }
                }
            }
            if(keywords.Length > 2) {
                method = new UmlMethod(keywords[0], keywords[1], keywords[2], inputParam);
            }
            return method;
        }
        private UmlBox readClass(string chunk) {
            return new UmlBox("Class", chunk.Substring(chunk.LastIndexOf(" ")), 0, 0);
        }
    }
}
