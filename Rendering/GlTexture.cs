﻿// Copyright Hugh Perkins 2006
//
// This program is free software; you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
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
using System.Runtime.InteropServices;
using System.Text;
//using System.Drawing;
//using System.Drawing.Imaging;
using Tao.OpenGl;
using Tao.DevIl;
using System.IO;

    // represents single OpenGl texture
    // could be abstracted via higher level interface
    public class GlTexture : ITexture
    {
        public int GlReference = 0;
        public int Width { get { return width; } }
        public int Height { get { return height; } }

        public byte[,] AlphaData
        {
            get
            {
                return alphadata;
            }
        }
        public string Filename
        {
            get { return filename; }
        }

        string filename = ""; 
        byte[,] alphadata; // the data inside the texture
        int width;
        int height;

        IGraphicsHelper g;

        public bool IsAlpha { get { return isalpha; } }
        public bool Modified { get { return modified; } set { modified = value; } }

        public bool isalpha = false;
        public bool modified = false;

        // note to self: do something with this
        //~GlTexture()
        //{
          //  Console.WriteLine("deleting texture ref " + GlReference + " " + filename);
//            Gl.glDeleteTextures(1, new int[] { GlReference });
  //      }

        // create blank texture
        public GlTexture(bool isalpha)
        {
            Image image = new Image(2, 2);
            Init(image, isalpha);
        }

        public GlTexture(Image image, bool isalpha)
        {
            this.filename = "";
            Init(image, isalpha);
        }

        public GlTexture(Image image, string filename, bool isalpha)
        {
            this.filename = filename;
            Init(image, isalpha);
        }

        // create blank texture
        public GlTexture(int width, int height, bool isalpha)
        {
            Image image = new Image(width, height);
            Init(image, isalpha);
        }

        void Init(Image image, bool isalpha)
        {
            this.isalpha = isalpha;
            g = GraphicsHelperFactory.GetInstance();
            this.width = image.Width;
            this.height = image.Height;
            if (isalpha)
            {
                GlReference = LoadImageToOpenGlAsAlpha(image);
            }
            else
            {
                GlReference = LoadImageToOpenGl(image);
            }
        }

        public static GlTexture FromFile(string filename)
        {
            Image image = new Image(filename);
            return new GlTexture(image, filename, false);
        }
        public static GlTexture FromAlphamapFile(string filename)
        {
            Image image = new Image(filename);
            return new GlTexture(image, filename, true);
        }

        public void LoadFromFile(string filename)
        {
            Gl.glDeleteTextures(1, new int[] { GlReference });
            GlReference = 0;
            Image image = new Image(filename);
            Init(image, IsAlpha );
            this.filename = filename;
        }
        
        public void SaveAlphaToFile(string filename)
        {
            Image image = new Image(width, height);
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    image.SetPixel(i, j, alphadata[i, j], alphadata[i, j], alphadata[i, j], alphadata[i, j]);
                }
            }
            image.Save(filename);
            this.filename = filename;
            this.modified = false;
        }

        public override string ToString()
        {
            return "GlTexture: " + filename + " " + width + " x " + height;
        }

        //sends instructions to opengl to use this texture
        public void Apply()
        {
            g.Bind2DTexture(GlReference);
        }

        public void ReloadAlpha()
        {
            byte[] data = new byte[width * height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    int offset = (j * width + i);
                    data[offset + 0] = alphadata[i,j];
                }
            }
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, GlReference);
            Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_ALPHA8, width, height, 0, Gl.GL_ALPHA, Gl.GL_UNSIGNED_BYTE, data);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_NEAREST);
        }

        int NextPowerOfTwo(int n)
        {
            double power = 0;
            while (n > Math.Pow(2.0, power))
                power++;
            return (int)Math.Pow(2.0, power);
        }

        int LoadImageToOpenGl(Image image)
        {
            width = image.Width;
            height = image.Height;
            LogFile.GetInstance().WriteLine( "loading texture " + filename + " width: " + width + " height: " + height);

            alphadata = new byte[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    alphadata[x, y] = image.GetAlpha( x, y );
                }
            }

            //int m_TextureWidth = NextPowerOfTwo(m_Width);
            //int m_TextureHeight = NextPowerOfTwo(m_Height);
            //if ((m_TextureWidth != m_Width) || (m_TextureHeight != m_Height))
              //  Ilu.iluEnlargeCanvas(m_TextureWidth, m_TextureHeight, m_Depth);

            //Ilu.iluFlipImage();

            int m_TextureID;
            Gl.glGenTextures(1, out m_TextureID);
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, m_TextureID);
            //Gl.glTexParameterf(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_NEAREST);
            //Gl.glTexParameterf(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_NEAREST);
            //Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, m_BytesPerPixel, m_TextureWidth,
            //    m_TextureHeight, 0, m_Format, Gl.GL_UNSIGNED_BYTE, dataptr );
            Glu.gluBuild2DMipmaps(Gl.GL_TEXTURE_2D, Gl.GL_RGBA8, width, height,
                Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, image.data);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);

            return m_TextureID;

            // from devil.net version of this function:
            //Glu.gluBuild2DMipmaps(Gl.GL_TEXTURE_2D, Gl.GL_RGBA8, bitmap.Width, bitmap.Height, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, data);
            //Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
        }

        // reads R channel as alpha channel
        int LoadImageToOpenGlAsAlpha( Image image )
        {
            width = image.Width;
            height = image.Height;
            LogFile.GetInstance().WriteLine("loading texture " + filename + " width: " + width + " height: " + height);

            alphadata = new byte[width, height];
            byte[] dataforgl = new byte[width * height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int gloffset = y * width + x;
                    alphadata[x, y] = image.GetRed(x, y);
                    dataforgl[gloffset] = image.GetRed(x, y);
                }
            }

            int iTextureReference;
            Gl.glGenTextures(1, out iTextureReference);
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, iTextureReference);
            Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_ALPHA8, width, height, 0, Gl.GL_ALPHA, Gl.GL_UNSIGNED_BYTE, dataforgl);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_NEAREST);

            return iTextureReference;
        }
    }
