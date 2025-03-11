using System.IO;
using System.Text.RegularExpressions;
namespace DocuDoctor.Model {


    /// <summary>
    /// Reads a file and outputs all read Classes
    /// </summary>
    public class Parser {

        private string m_file;
        private string[] m_keywords;
        private Syntax m_syntaxInfo;

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
            m_file = fileName;
            m_syntaxInfo = new Syntax(fileLangagueParser(fileName));
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

                //Initialize regex commands as objects I think this is faster than static methods
                Regex stringsAndComments = new Regex("\"[\\S\\s]*?\"|'[\\S\\s]*?'|\\/\\*[\\S\\s]*?\\*\\/|\\/\\/.*?[\\n]");

                Regex whiteSpace = new Regex("[ \t\n\r]+");
                using(StreamReader sr = new StreamReader(m_file)) {
                    while(!sr.EndOfStream) {
                        string line = sr.ReadLine();
                        fileContent += line + '\n';
                    }
                }
                fileContent = stringsAndComments.Replace(fileContent, " ");
                fileContent = whiteSpace.Replace(fileContent, " ");
            } catch(Exception ex) {
                if(ex is FileNotFoundException || ex is NullReferenceException) {
                    throw new FileNotFoundException("Invalid Input File");
                }
            }

            return fileContent;
        }

        private Langague fileLangagueParser(string filePath) {
            try {
                int test = filePath.LastIndexOf('.');
                string ending = filePath.Substring(filePath.LastIndexOf('.'));
                switch(ending) {
                    case ".cs":
                        return Langague.Csharp;
                    default:
                        return Langague.Invalid;
                }
                return Langague.Csharp;
            } catch(Exception ex) {
                if(ex is ArgumentOutOfRangeException)
                    return Langague.Invalid;
                else
                    throw;
            }
        }

        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: Parser : ParseFile                                    ::
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
            Regex findKeywords = new Regex(m_syntaxInfo.generateKeywordRegexCommand());//new Regex("(private |public |class |protected ).*?[);{]");
            MatchCollection keywordChunks = findKeywords.Matches(fileContent);

            List<UmlBox> result = new List<UmlBox>();

            foreach(Match match in keywordChunks) {
                //Assumes that the first thing in the list of matchs is a class as otherwise its real difficult to
                // associate a method/var with a class
                string chunk = match.Value;
                if(chunk.Contains('=') || (chunk.Contains(';') && !chunk.Contains(')'))) {
                    UmlVariable? temp = readVariable(chunk);
                    //Adds the found variable to the most recent UML box in the list
                    //should work for everthing except nested classes
                    if(temp != null && result.Count > 0 && result[result.Count - 1] != null) {
                        result[result.Count - 1].AddVariable(temp);
                    }

                } else if(m_syntaxInfo.isObject(chunk)) {
                    UmlBox? classBox = readClass(chunk);
                    if(classBox != null)
                        result.Add(classBox);
                } else if(m_syntaxInfo.isFunction(chunk)) {
                    //Adds the found variable to the most recent UML box in the list
                    //should work for everthing except nested classes
                    UmlMethod? method = readMethod(chunk);
                    if(method != null && result.Count > 0 && result[result.Count - 1] != null) {
                        result[result.Count - 1].AddMethod(method);
                    }

                }
            }
            return result;
        }



        private UmlVariable? readVariable(string text) {
            //TODO: change this into a something better
            int removeIndex = Math.Max(text.IndexOf('='), text.IndexOf(';'));
            if(removeIndex > 0)
                text = text.Remove(removeIndex);
            string[] chunks = text.Split(' ');
            if(chunks.Length >= 3)
                return new UmlVariable(chunks[0], chunks[1], chunks[2]);
            return null;
        }

        private UmlMethod? readMethod(string chunk) {
            // Seperate all the parameters for the method (assumes we use , for seperation)
            string[] parameters = Regex.Match(chunk, "(?<=\\().*?(?=\\))").Value.Split(",");

            //Gets rid of the parmeters from the method text
            chunk = Regex.Replace(chunk, "\\(.*?\\)", " ");
            //breaks the method into protection return type and name
            string[] keywords = chunk.Split(" ");

            UmlMethod method = null;
            List<UmlVariable> inputParam = [];
            foreach(string param in parameters) {
                if(param.Length > 0) {
                    //Finds the point between the type and the name of the parameter, alaways assuming that space is
                    // a delimenatar(god I can't spell)
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
            if(keywords.Length >= 3) {
                method = new UmlMethod(keywords[0], keywords[1], keywords[2], inputParam);
            }
            return method;
        }
        private UmlBox? readClass(string chunk) {
            //TODO: This is all a bit of a mess and needs to be improved 
            if(chunk.Contains(m_syntaxInfo.functionEnding)) {
                chunk = chunk.Remove(chunk.LastIndexOf(m_syntaxInfo.functionEnding)).Trim();
            }
            string[] keywords = chunk.Split(" ");
            if(keywords.Length >= 2)
                return new UmlBox(keywords[0], keywords[1], 0, 0);
            return null;
        }

        public class Syntax {
            public string[] visibility;
            public string[] objectKeywords;
            public string objectEnding;
            public string varEnding;
            public string functionEnding;
            //Should store Comment/string info

            public Syntax(Langague langague) {
                //This is a real ugly
                switch(langague) {
                    case Langague.Csharp:
                        visibility = ["private", "public", "protected"];
                        objectKeywords = ["class"];
                        objectEnding = "{";
                        varEnding = ";";
                        functionEnding = ")";
                        break;
                    default:
                        visibility = [];
                        objectKeywords = [];
                        objectEnding = "";
                        varEnding = "";
                        functionEnding = "";
                        break;
                }

            }

            /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
            :: 1. Method: Parser : ParseFile                                    ::
            :: ---------------------------------------------------------------- ::
            :: 2. Author: Riley Horling                                         ::
            :: 3. Created: 2/10/2025                                            ::
            :: 4. Purpose: Creates a custom regex command to parse the file     ::
            :: ---------------------------------------------------------------- ::
            :: 6. Output Parameters: regex command                              ::
            :: 7. Preconditions: None                                           ::
            :: 8. Throws: None                                                  ::
            :: ---------------------------------------------------------------- ::
            :: 9. Modifications: None                                           ::
            ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
            public string generateKeywordRegexCommand() {
                //Example command for C#: (private |public |class |protected ).*?[);{]
                string command = "(";


                foreach(string item in visibility) {
                    command += item + " |";
                }
                foreach(string item in objectKeywords) {
                    command += item + " |";
                }
                //Lazy solution
                if(command.EndsWith('|'))
                    command = command.Substring(0, command.Length - 1);

                command += ").*?[";
                command += objectEnding + varEnding + functionEnding + "]";
                return command;
            }

            public bool isObject(string text) {
                foreach(string item in objectKeywords) {
                    if(text.Contains(item + " "))
                        return true;
                }
                return false;
            }

            public bool isFunction(string text) {
                return text.Contains(functionEnding);
            }

            public bool isVar(string text) {
                return text.Contains(varEnding);
            }
        }

        public enum Langague {
            Csharp, Invalid
        }
    }
}
