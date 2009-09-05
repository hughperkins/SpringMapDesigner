// Copyright Hugh Perkins 2006
// hughperkins@gmail.com http://manageddreams.com
//
// This program is free software; you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURVector3E. See the GNU General Public License for
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

namespace MapDesigner
{
    public class ComboKeyEventArgs : EventArgs
    {
        public bool IsComboDown;
        public ComboKeyEventArgs( bool iscombodown )
        {
            this.IsComboDown = iscombodown;
        }
    }
    
    public delegate void KeyComboHandler( object source, ComboKeyEventArgs e );
  
    // call RegisterCombo( string[] Combo, string[] AllowedModifiers, KeyComboHandler keycombohandler )
    // to register a callback for a specific key command combo
    // the key commands are mapped to specific keyboard keys in the config file (see Config.cs)    
    // combos are not considered matched unless they match exactly, or any extra keys are included in AllowedModifiers
    //
    // Specify Combo as {"none"} to be informed when nothing is held down;  this has the effect of informing you
    // when anything is pressed except AllowedModifiers.  We use this in PlayerMovement for example so that you only
    // mouselook when no keys are pressed except movement keys (ie not editmode etc...)
    // Pass in AllowedModifiers to {"any"} to allow any modifier
    //
    public class KeyFilterComboKeys
    {
        IKeyFilterConfigMappings keyfilterconfigmappings;

        List<ComboInfo> combos = new List<ComboInfo>();

        List<ComboInfo> currentmatchedcombos = new List<ComboInfo>();
        List<ComboInfo> oldmatchedcombos = new List<ComboInfo>();
        
        static KeyFilterComboKeys instance = new KeyFilterComboKeys();
        
        class ComboInfo
        {
            public List<String> Combo;
            public List<String> AllowedModifiers;
            public KeyComboHandler handler;
            public ComboInfo( List<String> Combo, List<String> AllowedModifiers, KeyComboHandler handler )
            {
                this.Combo = Combo; this.AllowedModifiers = AllowedModifiers; this.handler = handler;
            }
        }
        
        public static KeyFilterComboKeys GetInstance()
        {
            return instance;
        }
        List<String> testlist = new List<String>();
        public KeyFilterComboKeys()
        {
            keyfilterconfigmappings = KeyFilterConfigMappingsFactory.GetInstance();
            keyfilterconfigmappings.KeyEvent += new KeyConfigMappingEventHandler( KeyEvent );            
        }
        
        public void KeyEvent( object source, KeyMappingEventArgs e )
        {
            List<String> changedcommandkeys = e.CommandsChanged;
            Console.WriteLine( String.Join( ",", changedcommandkeys.ToArray()) );
            bool bKeyDown = e.IsKeyDown;
            
            oldmatchedcombos = currentmatchedcombos;
            currentmatchedcombos = new List<ComboInfo>();
            
            // scan combo objects, checking that combo keys are pressed, but extra keys not in modifiers are not
            // add these combos to currentcombos
            foreach( object comboobject in combos )
            {
                ComboInfo combo = (ComboInfo)comboobject;
                if( IsComboDown( combo.Combo, combo.AllowedModifiers ) )
                {
                    currentmatchedcombos.Add( combo );
                }
            }

            List<ComboInfo> CombosSet = new List<ComboInfo>();
            List<ComboInfo> CombosUnset = new List<ComboInfo>();
            
            // compare currentcombos with oldcombos, add differences to CombosSet and CombosUnset
            // first CombosSet
            foreach (ComboInfo currentmatchedcombo in currentmatchedcombos)
            {
                if (!oldmatchedcombos.Contains(currentmatchedcombo))
                {
                    CombosSet.Add(currentmatchedcombo);
                }
            }
            // CombosUnset:
            foreach (ComboInfo oldmatchedcombo in oldmatchedcombos)
            {
                if (!currentmatchedcombos.Contains(oldmatchedcombo))
                {
                    CombosUnset.Add(oldmatchedcombo);
                }
            }
            
            // send out events...
            foreach (ComboInfo comboinfo in CombosUnset)
            {
                comboinfo.handler(this, new ComboKeyEventArgs(false));
                Test.WriteOut("released combo: " + comboinfo.Combo.ToString());
            }
            foreach (ComboInfo comboinfo in CombosSet)
            {
                comboinfo.handler( this, new ComboKeyEventArgs( true ) );
                Test.WriteOut("new combo: " + comboinfo.Combo.ToString());
            }
        }
        public bool IsComboDown( List<String> Combo, List<String> AllowedModifiers )
        {
            //List<String> AllPressedCommandKeys = testlist;
            List<String> AllPressedCommandKeys = keyfilterconfigmappings.AllPressedCommandKeys;
            Dictionary<string,List<string>> AllPressedCommandKeysByKeyCode = keyfilterconfigmappings.AllPressedCommandKeysByKeyCode;
            
            bool allpressedcontainscombo = true;
            bool allpressedcontainsillegalmodifiers = false;
            if( Combo.Count == 1 && ( Combo.Contains("none") || Combo.Contains("NONE") ) )
            {
                allpressedcontainscombo = true;
            }
            else
            {
                for( int i = 0; i < Combo.Count && allpressedcontainscombo; i++ )
                {
                    if( !AllPressedCommandKeys.Contains( Combo[i] ) )
                    {
                        allpressedcontainscombo = false;
                    }
                }
            }
            
            if( allpressedcontainscombo )
            {
                if(  AllowedModifiers.Count == 1 && ( AllowedModifiers.Contains( "any" ) || AllowedModifiers.Contains( "ANY" ) ) )
                {
                    allpressedcontainsillegalmodifiers = false;
                }
                else
                {
                    foreach (List<String> onekeycodestrings in AllPressedCommandKeysByKeyCode.Values )
                    {
                        bool bThisKeyOk = false;
                        foreach( string commandstring in onekeycodestrings )
                        {
                            if( Combo.Contains( commandstring ) ||
                                AllowedModifiers.Contains( commandstring ) )
                            {
                                bThisKeyOk = true;
                            }
                        }
                        if( !bThisKeyOk )
                        {
                            allpressedcontainsillegalmodifiers = true;
                        }
                    }
                }
            }
            if( allpressedcontainscombo && !allpressedcontainsillegalmodifiers )
            {
                return true;
            }
            return false;
        }
        public void RegisterCombo( string[] Combo, string[] AllowedModifiers, KeyComboHandler keycombohandler )
        {
            Test.WriteOut( "RegisterCombo: " + Combo[0] );
            combos.Add( new ComboInfo( new List<String>( Combo ), new List<String>( AllowedModifiers ), keycombohandler ) );
        }
    }
}
