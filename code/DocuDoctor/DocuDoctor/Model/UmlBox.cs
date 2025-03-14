namespace DocuDoctor.Model {
    /// <summary>
    /// Container Object For UML Items (aka Classes, Templates, Interfaces)
    /// </summary>

    public class UmlBox
    {
        // Type of box (class, template, interface)
        private string m_boxType;
        public string BoxType { get { return m_boxType; } set { m_boxType = value; } }

        // Current name of the box
        private string m_name;
        public string Name { get { return m_name; } set { m_name = value; } }

        // List of variables that the container type contains
        private List<UmlVariable> m_variables;
        public List<UmlVariable> Variables { get { return m_variables; } }
        // List of methods that the container type contains
        private List<UmlMethod> m_methods;
        public List<UmlMethod> Methods { get { return m_methods; } }

        // variables used and stored for display on refresh of screen
        private float m_x;
        public float X { get { return m_x; } set { m_x = value; } }
        private float m_y;
        public float Y { get { return m_y; } set { m_y = value; } }
        private float m_width;
        public float Width { get { return m_width; } set { m_width = value; } }
        private float m_height;
        public float Height { get { return m_height; } set { m_height = value; } }

        // Id will be randomized and assinged on initialize.
        // By using a long, the chance of a collision is as close to 0 as imaginable
        private long m_id;
        public long ID { get { return m_id; } }

        private List<(long, int)> m_arrows;
        public List<(long, int)> Arrows { get { return m_arrows; } }

        public UmlBox(string type, string name, int x, int y)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: UmlBox : UmlBox                                       ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Initializer for UML Boxes                            ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {

            m_boxType = type; m_name = name; m_variables = []; m_methods = [];
            m_x = x; m_y = y; m_width = 0; m_height = 1;
            // Initialize a random id to show which 
            Random rnd = new Random();
            m_id = rnd.NextInt64();
            m_arrows = new List<(long, int)>();
        }

        public void AddMethod(string protection, string returnType, string name, List<List<String>> parameters)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: AddMethod : UmlBox                                    ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Add method to the current box                        ::
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

        public void AddVariable(string protection, string type, string name)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: AddVariable : UmlBox                                  ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Add variable to the current box                      ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            m_variables.Add(new UmlVariable(protection, type, name));
        }

        public void AddArrow(long id, int type)
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: AddArrow : UmlBox                                     ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Add arrow to the current box                         ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            // Only add id if it is new
            foreach ((long, int) i in m_arrows) {
                if (id == i.Item1) return;
            }
            m_arrows.Add((id, type));
        }

        public override string ToString()
        /*::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        :: 1. Method: ToString : UmlBox                                     ::
        :: ---------------------------------------------------------------- ::
        :: 2. Author: Christopher Villanueva                                ::
        :: 3. Purpose: Overrides ToString allowing for easier display       ::
        ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::*/
        {
            return m_boxType + " : " + m_name;
        }
    }
}
