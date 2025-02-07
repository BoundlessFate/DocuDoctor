namespace DocuDoctor.Model {
    /// <summary>
    /// Container Object For UML Items (aka Classes, Templates, Interfaces)
    /// </summary>
    public class UmlBox {
        private string m_boxType;
        public string BoxType { get { return m_boxType; } set { m_boxType = value; } }

        private string m_name;
        public string Name { get { return m_name; } set { m_name = value; } }

        private List<UmlVariable> m_variables;
        public List<UmlVariable> Variables { get { return m_variables; } }
        private List<UmlMethod> m_methods;
        public List<UmlMethod> Methods { get { return m_methods; } }

        private float m_x;
        public float X { get { return m_x; } set { m_x = value; } }
        private float m_y;
        public float Y { get { return m_y; } set { m_y = value; } }
        private float m_width;
        public float Width { get { return m_width; } set { m_width = value; } }
        private float m_height;
        public float Height { get { return m_height; } set { m_height = value; } }

        public UmlBox(string type, string name, int x, int y)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: UmlBox : UmlBox                                       ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 1/10/2025                                            ::
        :: 4. Purpose: Initializer for UML Boxes                            ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: type - type of box                          ::
        ::                      name - name of the box                      ::
        ::                         x - x coordinate of the box              ::
        ::                         y - y coordinate of the box              ::
        :: 6. Output Parameters: None                                       ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            m_boxType = type;
            m_name = name;
            m_variables = [];
            m_methods = [];
            m_x = x;
            m_y = y;
            m_width = 0;
            m_height = 1;
        }



        public void AddMethod(string protection, string returnType, string name, List<List<String>> parameters)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: AddMethod : UmlBox                                    ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 1/10/2025                                            ::
        :: 4. Purpose: Add method to the current box                        ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: protection - protection level of the method ::
        ::                      returnType - return type of the method      ::
        ::                            name - name of the method             ::
        ::                      parameters - parameters of the method       ::
        :: 6. Output Parameters: None                                       ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            m_methods.Add(new UmlMethod(protection, returnType, name, parameters));
        }

        public void AddMethod(UmlMethod newMethod) {
            m_methods.Add(newMethod);
        }

        public void AddVariable(UmlVariable newVariable) {
            m_variables.Add(newVariable);
        }

        public void AddVariable(string protection, string type, string name) {
            m_variables.Add(new UmlVariable(protection, type, name));
        }

        public override string ToString()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: ToString : UmlBox                                     ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Created: 1/10/2025                                            ::
        :: 4. Purpose: Overrides ToString allowing for easier display       ::
        :: ---------------------------------------------------------------- ::
        :: 5. Input Parameters: None                                        ::
        :: 6. Output Parameters: string containing data about box           ::
        :: 7. Preconditions: None                                           ::
        :: 8. Throws: None                                                  ::
        :: ---------------------------------------------------------------- ::
        :: 9. Modifications: None                                           ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            return m_boxType + " : " + m_name;
        }
    }
}
