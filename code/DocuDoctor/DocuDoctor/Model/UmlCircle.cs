using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocuDoctor.Model
{
    public class UmlCircle
    {


        //variables for display purposes
        private float m_x;
        public float X { get { return m_x; } set { m_x = value; } }
        private float m_y;
        public float Y { get { return m_y; } set { m_y = value; } }
        private float m_width;
        public float Width { get { return m_width; } set { m_width = value; } }
        private float m_height;
        public float Height { get { return m_height; } set { m_height = value; } }

        private long m_id;
        public long ID { get { return m_id; } }




        public UmlCircle()
        {

            m_width = 0; m_height = 1;
            // Initialize a random id to show which 
            Random rnd = new Random();
            m_id = rnd.NextInt64();

        }

        public UmlCircle(int x, int y)
        {

            m_width = 1; m_height = 1;
            m_x = x; m_y = y; 
            // Initialize a random id to show which 
            Random rnd = new Random();
            m_id = rnd.NextInt64();
        }

        public void ChangeWidth(int newWidth)
        {
            m_width = newWidth;
        }

        public void ChangeHeight(int newHeight)
        {
            m_height = newHeight;
        }

    }
}
