using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sandtris
{
    public class TransparentPictureBox : PictureBox
    {
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Do not paint background to keep it transparent
        }
    }
}
