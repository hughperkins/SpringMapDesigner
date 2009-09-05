﻿// Copyright Hugh Perkins 2006
// hughperkins@gmail.com http://manageddreams.com
//
// This program is free software; you can redistribute it and/or modify it
// under the terms of the GNU General Public License version 2 as published by the
// Free Software Foundation;
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for
//  more details.
//
// You should have received a copy of the GNU General Public License along
// with this program in the file licence.txt; if not, write to the
// Free Software Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-
// 1307 USA
// You can find the licence also on the web at:
// http://www.opensource.org/licenses/gpl-license.php
//

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Gtk;
using Glade;

namespace MapDesigner
{
    class HeightScaleDialog
    {
        [Widget]
        Entry minimumheightentry = null;

        [Widget]
        Entry maximumheightentry = null;

        [Widget]
        RadioButton radioScale = null;

        //[Widget]
        //RadioButton radioClip = null;

        [Widget]
        Window heightscaledialog = null;

        void on_btnok_clicked(object o, EventArgs e)
        {
            try
            {
                double minimumheight = Convert.ToDouble(minimumheightentry.Text);
                double maximumheight = Convert.ToDouble(maximumheightentry.Text);
                if (minimumheight >= maximumheight)
                {
                    DialogHelpers.ShowErrorMessage(heightscaledialog, "Maximum height should be greater than minimum height");
                    return;
                }
                bool Scale = radioScale.Active;
                //CmdHeightScaleChange.Operation operation = CmdHeightScaleChange.Operation.Scale;
                //if (clip)
                //{
                    //operation = CmdHeightScaleChange.Operation.Clip;
                //}
                //Console.WriteLine(operation);
                //heightscaledialog.Hide();

                Terrain.GetInstance().ChangeHeightScale(minimumheight, maximumheight, Scale);

                heightscaledialog.Destroy();
            }
            //catch( Exception ex )
            catch
            {
                //Console.WriteLine(ex);
                DialogHelpers.ShowErrorMessage(heightscaledialog, "Maximum height should be greater than minimum height");
                return;
            }
        }

        public HeightScaleDialog()
        {
            Glade.XML app = new Glade.XML("./MapDesigner.glade", "heightscaledialog", "");
            app.Autoconnect(this);
            minimumheightentry.Text = Terrain.GetInstance().MinHeight.ToString();
            maximumheightentry.Text = Terrain.GetInstance().MaxHeight.ToString();
        }
    }
}
