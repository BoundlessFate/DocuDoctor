namespace DocuDoctor.Model {
    /// <summary>
    /// Common interface for variables and general items
    /// </summary>
    public interface UmlItem {
        public string Protection { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// Data object for variables
    /// </summary>
    public class UmlVariable : UmlItem {
        protected string m_protection;
        public string Protection { get { return m_protection; } set { m_protection = value; } }
        protected string m_type;
        public string Type { get { return m_type; } set { m_type = value; } }
        protected string m_name;
        public string Name { get { return m_name; } set { m_name = value; } }

        public UmlVariable(string protection, string type, string name)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: UmlVariable : UmlVariable                             ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 1/10/2025                                            ::
        :: 4. Purpose: Initializer for variables                            ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: protection - protection level               ::
        ::                            type - type of variable               ::
        ::                            name - name of variable               ::
        :: 6. Output Parameters: None                                       ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            m_protection = protection;
            m_type = type;
            m_name = name;
        }


        public override string ToString()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: ToString : UmlVariable                                ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 1/10/2025                                            ::
        :: 4. Purpose: Overrides ToString allowing for easier display       ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: None                                        ::
        :: 6. Output Parameters: string containing data about variable      ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            if(m_protection.Equals(""))
                return m_type + " " + m_name;
            return m_protection + " " + m_type + " " + m_name;
        }
    }

    /// <summary>
    /// Data object for methods
    /// </summary>
    public class UmlMethod : UmlVariable {
        protected List<UmlVariable> m_parameters;
        public List<UmlVariable> Parameters { get { return m_parameters; } set { m_parameters = value; } }
        public UmlMethod(string protection, string returnType, string name, List<List<String>> parameters) : base(protection, returnType, name)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: UmlMethod : UmlMethod                                 ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 1/10/2025                                            ::
        :: 4. Purpose: Initializer for methods                              ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: protection - protection level               ::
        ::                      returnType - return type of method          ::
        ::                            name - name of method                 ::
        ::                      parameters - input parameters               ::
        :: 6. Output Parameters: None                                       ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            m_parameters = [];
            try {
                foreach(List<String> param in parameters) {
                    if(param.Count != 2)
                        throw new Exception("parameter does not have the right num of arguments");
                    foreach(string str in param) {
                        if(str.Equals(null) || str.Equals(""))
                            throw new Exception("parameter value cannot be null or empty");
                    }
                }
                foreach(List<String> param in parameters) {
                    m_parameters.Add(new UmlVariable("", (string)param[0]!, (string)param[1]!));
                }
            } catch(Exception ex) {
                Console.WriteLine(ex.Message, ex.StackTrace);
            }
        }

        public UmlMethod(string protection, string returnType, string name, List<UmlVariable> parameters) : base(protection, returnType, name) {
            m_parameters = parameters;
        }

        public override string ToString()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: ToString : UmlMethod                                  ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 1/10/2025                                            ::
        :: 4. Purpose: Overrides ToString allowing for easier display       ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: None                                        ::
        :: 6. Output Parameters: string containing data about method        ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            string str = m_protection + " " + m_type + " " + m_name + " (";
            for(int i = 0; i < m_parameters.Count; i++) {
                if(i != 0)
                    str += ", ";
                str += m_parameters[i].ToString();
            }
            str += ")";
            return str;
        }
    }
}
