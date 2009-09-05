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
using System.Text;
using Tao.OpenGl;

namespace MapDesigner
{
    // encompasses a heightmap, and contains logic to render it
    public class RenderableHeightMap
    {
        double[,] heightmap;
        int width; // doesnt include 1 pixel border around edge
        int height; // doesnt include 1 pixel border around edge
        int chunksize = 32; // used for culling etc
        // each chunk is rendered separately
        int xscale = 8; // used for rendering to increase rendered map area by this ratio
        int yscale = 8;

        //const int[] loddistances = new int[] { 1200, 2400, 4000, 5000, 8000 };
        public int[] loddistances = new int[] { 400, 600, 800, 1000, 1200, 1400 };

        int maxtexels;
        bool multipass = false;
        List<RendererPass> rendererpasses;

        Terrain parent;

        /// <summary>
        /// Cache of normal of each quad, indexed by topleft of quad, at resolution of heightmap.
        /// The gl normals will take the average of each adjacent quad, to ensure smooth normals
        /// </summary>
        public Vector3[,] normalsperquad;

        List<MapTextureStage> maptexturestages;

        Dictionary<MapTextureStage, bool[,]> chunkusestexturestage = new Dictionary<MapTextureStage, bool[,]>();

        // pass a pointer to a heightmap
        // when rendered, x pos will be multiplied by xscale, and y points by yscale
        public RenderableHeightMap(Terrain parent, double[,] heightmap, int xscale, int yscale, List<MapTextureStage> maptexturestages)
        {
            this.parent = parent;
            this.heightmap = heightmap;
            width = heightmap.GetLength(0) - 1;
            height = heightmap.GetLength(1) - 1;
            this.xscale = xscale;
            this.yscale = yscale;
            this.maptexturestages = maptexturestages;
            CacheChunkTextureStageUsage();
            normalsperquad = new Vector3[width, height];
            terrain_HeightmapInPlaceEdited(0, 0, width - 1, height - 1);
            RendererFactory.GetInstance().WriteNextFrameEvent += new WriteNextFrameCallback(Render);
            parent.HeightmapInPlaceEdited += new Terrain.HeightmapInPlaceEditedHandler(terrain_HeightmapInPlaceEdited);
            parent.TerrainModified += new Terrain.TerrainModifiedHandler(terrain_TerrainModified);
            parent.BlendmapInPlaceEdited += new Terrain.BlendmapInPlaceEditedHandler(terrain_BlendmapInPlaceEdited);
        }

        void terrain_HeightmapInPlaceEdited(int xleft, int ytop, int xright, int ybottom)
        {
            //LogFile.GetInstance().WriteLine("RHM data changed " + xleft + " " + ytop + " " + xright + " " + ybottom);
            for (int mapx = xleft; mapx <= xright; mapx++)
            {
                for (int mapy = ytop; mapy <= ybottom; mapy++)
                {
                    Vector3 a = new Vector3(xscale, yscale,
                        heightmap[mapx + 1, mapy + 1] -
                        heightmap[mapx, mapy]);
                    Vector3 b = new Vector3(xscale, -yscale,
                        heightmap[mapx + 1, mapy] -
                        heightmap[mapx, mapy + 1]);
                    Vector3 normal = Vector3.CrossProduct(a, b).Normalize();
                    normalsperquad[mapx, mapy] = normal;
                }
            }
        }

        void terrain_BlendmapInPlaceEdited(MapTextureStage maptexturestage, int xleft, int ytop, int xright, int ybottom)
        {
            for (int chunkx = xleft / chunksize; chunkx <= xright / chunksize && chunkx < width / chunksize;
                chunkx++)
            {
                for (int chunky = ytop / chunksize; chunky <= ybottom / chunksize && chunky < height / chunksize;
                    chunky++)
                {
                    //    Console.WriteLine("chunk " + chunkx + " " + chunky);
                    //for (int texturestageindex = 0; texturestageindex < maptexturestages.GetLength(0); texturestageindex++)
                    //{
//                        maptexturestage = maptexturestages[texturestageindex];
                    if (maptexturestage.Operation == MapTextureStage.OperationType.Blend)
                    {
                        bool texturestageused = false;
                        // go through each point in chunk and check if texturestage is used
                        for (int mapx = chunkx * chunksize; mapx < (chunkx + 1) * chunksize && !texturestageused;
                            mapx++)
                        {
                            for (int mapy = chunky * chunksize; mapy < (chunky + 1) * chunksize && !texturestageused;
                                mapy++)
                            {
                                //if (mapx < width && mapy < height)
                                //{
                                    if (maptexturestage.Affects(mapx, mapy, width, height))
                                    {
                                        texturestageused = true;
                                    }
                                //}
                            }
                        }
                        chunkusestexturestage[maptexturestage][chunkx, chunky] = texturestageused;
                        //  if (chunkusestexturestage[texturestageindex][chunkx, chunky])
                        //{
                        //  Console.WriteLine("texturestage used: " + texturestageindex + " " + chunkx + " " + chunky);
                        //}
                    }
                    else
                    {
                        chunkusestexturestage[maptexturestage][chunkx, chunky] = true;
                    }
                    //}
                }
            }
        }

        void terrain_TerrainModified()
        {
            this.maptexturestages = parent.texturestages;
            this.heightmap = parent.Map;
            width = heightmap.GetLength(0) - 1;
            height = heightmap.GetLength(1) - 1;
            CacheChunkTextureStageUsage();
            normalsperquad = new Vector3[width, height ];
            terrain_HeightmapInPlaceEdited(0, 0, width - 1, height - 1 );
        }

        // determine which chunks are used by each texture stage
        // only affects blend really
        void CacheChunkTextureStageUsage()
        {
            int numxchunks = width / chunksize;
            int numychunks = height / chunksize;
            chunkusestexturestage = new Dictionary<MapTextureStage, bool[,]>();
            foreach(MapTextureStage maptexturestage in maptexturestages )
            {
                chunkusestexturestage.Add(maptexturestage, new bool[numxchunks, numychunks]);
            }
            for (int chunkx = 0; chunkx < numxchunks; chunkx++)
            {
                for (int chunky = 0; chunky < numychunks; chunky++)
                {
                    //    Console.WriteLine("chunk " + chunkx + " " + chunky);
                    for (int texturestageindex = 0; texturestageindex < maptexturestages.Count; texturestageindex++)
                    {
                        MapTextureStage texturestage = maptexturestages[texturestageindex];
                        //Console.WriteLine("texturestage " + texturestageindex + " " + texturestage.Operation );
                        if (texturestage.Operation == MapTextureStage.OperationType.Blend)
                        {
                            bool texturestageused = false;
                            // go through each point in chunk and check if texturestage is used
                            for (int mapx = chunkx * chunksize; mapx < (chunkx + 1) * chunksize && !texturestageused;
                                mapx++)
                            {
                                for (int mapy = chunky * chunksize; mapy < (chunky + 1) * chunksize && !texturestageused;
                                    mapy++)
                                {
                                    if (texturestage.Affects(mapx, mapy, width, height))
                                    {
                                        texturestageused = true;
                                    }
                                }
                            }
                            chunkusestexturestage[texturestage][chunkx, chunky] = texturestageused;
                            //  if (chunkusestexturestage[texturestageindex][chunkx, chunky])
                            //{
                            //  Console.WriteLine("texturestage used: " + texturestageindex + " " + chunkx + " " + chunky);
                            //}
                        }
                        else if (texturestage.Operation == MapTextureStage.OperationType.Nop)
                        {
                            chunkusestexturestage[texturestage][chunkx, chunky] = false;
                        }
                        else
                        {
                            //Console.WriteLine("RenderableHeightMap,, cache chunk usage, true");
                            chunkusestexturestage[texturestage][chunkx, chunky] = true;
                        }
                    }
                }
            }
            maxtexels = RendererSdl.GetInstance().MaxTexelUnits;

            int totaltexturestagesneeded = 0;
            foreach (MapTextureStage maptexturestage in maptexturestages)
            {
                totaltexturestagesneeded += maptexturestage.NumTextureStagesRequired;
            }

            multipass = false;
            if (totaltexturestagesneeded > maxtexels)
            {
                multipass = true;
            }
            rendererpasses = new List<RendererPass>();
            //maxtexels = 2;
            //int currenttexel = 0;

            multipass = true; // force multipass for now for simplicity
            if (multipass)
            {
                for (int i = 0; i < maptexturestages.Count; i++)
                {
                    MapTextureStage maptexturestage = maptexturestages[i];
                    int numtexturestagesrequired = maptexturestage.NumTextureStagesRequired;
                    if (numtexturestagesrequired > 0) // exclude Nops
                    {
                        RendererPass rendererpass = new RendererPass(maxtexels);
                        for (int j = 0; j < maptexturestage.NumTextureStagesRequired; j++)
                        {
                            rendererpass.AddStage(new RendererTextureStage(maptexturestage, j, true, width, height));
                        }
                        rendererpasses.Add(rendererpass);
                    }
                }
            }
        }

        // renders to 0,0,0 ; size will be (mapwidth + 1) * xscale by (mapheight + 1) * yscale
        public void Render(Vector3 camerapos)
        {
            //ExportAsSingleTexture.GetInstance().Export("");

            Gl.glPushMatrix();
            FrustrumCulling culling = FrustrumCulling.GetInstance();
            //IGraphicsHelper g = GraphicsHelperFactory.GetInstance();
            GraphicsHelperGl g = new GraphicsHelperGl();

            int iSectorsDrawn = 0;
            int iChunkDrawsSkippedNoTextureSection = 0;

            int numxchunks = width / chunksize;
            int numychunks = height / chunksize;

            double chunkboundingradius = chunksize * xscale * 1.414 / 2;
            //Console.WriteLine("chunkboundingradius: " + chunkboundingradius);

            g.SetMaterialColor(new Color(1.0, 1.0, 1.0));

            Gl.glDepthFunc(Gl.GL_LEQUAL);
            g.EnableBlendSrcAlpha();

            foreach( RendererPass rendererpass in rendererpasses )
            {
                rendererpass.Apply();
                for (int chunkx = 0; chunkx < numxchunks - 1; chunkx++)
                {
                    for (int chunky = 0; chunky < numychunks - 1; chunky++)
                    {
                        if (chunkusestexturestage[rendererpass.texturestages[0].maptexturestage][chunkx, chunky])
                        {
                            //if (iSectorsDrawn == 0)
                            //{
                            int chunkmapposx = chunkx * chunksize;
                            int chunkmapposy = chunky * chunksize;
                            int chunkdisplayposx = chunkmapposx * xscale;
                            int chunkdisplayposy = chunkmapposy * yscale;
                            Vector3 chunkmappos = new Vector3(chunkmapposx, chunkmapposy, heightmap[chunkmapposx, chunkmapposy]);
                            Vector3 chunkdisplaypos = new Vector3(chunkdisplayposx, chunkdisplayposy, heightmap[chunkmapposx, chunkmapposy]);
                            Vector3 chunkcentredisplaypos = chunkdisplaypos +
                                new Vector3(chunksize * xscale / 2, chunksize * yscale / 2, 0);
                            if (culling.IsInsideFrustum(chunkcentredisplaypos, chunkboundingradius))
                            {
                                iSectorsDrawn++;

                                // check how far away sector is
                                // if nearby we render it in detail
                                // otherwise just render a few points from it
                                double distancesquared = Vector3.DistanceSquared(chunkcentredisplaypos, camerapos);
                                //if ( distancesquared > detaildistance * detaildistance)
                                int stepsize = 16;
                                if (distancesquared < loddistances[0] * loddistances[0])
                                {
                                    stepsize = 1;
                                }
                                else if (distancesquared < loddistances[1] * loddistances[2])
                                {
                                    stepsize = 2;
                                }
                                else if (distancesquared < loddistances[3] * loddistances[3])
                                {
                                    stepsize = 4;
                                }
                                else if (distancesquared < loddistances[4] * loddistances[4])
                                {
                                    stepsize = 8;
                                }
                                else if (distancesquared < loddistances[5] * loddistances[5])
                                {
                                   stepsize = 16;
                                }

                                RenderChunk(chunkx, chunky, stepsize);
                            }
                        }
                        else
                        {
                            iChunkDrawsSkippedNoTextureSection++;
                        }
                        // System.Environment.Exit(1);
                    }
                }
                //Gl.glTranslated(0, 0, 500);
            }
            //System.Environment.Exit(0);
            //Console.WriteLine("chunk renders: " + iSectorsDrawn + " maptextureculls: " + iChunkDrawsSkippedNoTextureSection);
            for (int i = maxtexels - 1; i >= 0; i--)
            {
                g.ActiveTexture(i);
                g.SetTextureScale(1 );
                g.DisableTexture2d();
            }
            Gl.glDepthFunc(Gl.GL_LESS);
            Gl.glDisable(Gl.GL_BLEND);
            g.EnableModulate();
            Gl.glPopMatrix();
        }

        void RenderChunk(int chunkx, int chunky, int step)
        {
            //Console.WriteLine("render chunk " + chunkx + " " + chunky + " " + step);
            Vector3 normal;
            for (int mapx = chunkx * chunksize; mapx <= (chunkx + 1) * chunksize; mapx += step)
            {
                Gl.glBegin(Gl.GL_TRIANGLE_STRIP);
                for (int mapy = (chunky + 1) * chunksize; mapy >= chunky * chunksize; mapy -= step)
                {
                    normal = GetNormal(mapx, mapy);
                    Gl.glNormal3d(normal.x, normal.y, normal.z);

                    //float u = (float)mapx / width;
                    //float v = (float)mapy / height;
                    float u = (float)mapx;
                    float v = (float)mapy;
                    Gl.glTexCoord2f(u, v);
                    Gl.glMultiTexCoord2fARB(Gl.GL_TEXTURE1_ARB, u, v);
                    Gl.glMultiTexCoord2fARB(Gl.GL_TEXTURE2_ARB, u, v);
                    Gl.glMultiTexCoord2fARB(Gl.GL_TEXTURE3_ARB, u, v);

                    Gl.glVertex3d(mapx * xscale, mapy * yscale, heightmap[mapx, mapy]);

                    normal = GetNormal(mapx + step, mapy);
                    Gl.glNormal3d(normal.x, normal.y, normal.z);

                    u = (float)mapx + step;
                    //v = (float)mapy;
                    Gl.glTexCoord2f(u, v);
                    Gl.glMultiTexCoord2fARB(Gl.GL_TEXTURE1_ARB, u, v);
                    Gl.glMultiTexCoord2fARB(Gl.GL_TEXTURE2_ARB, u, v);
                    Gl.glMultiTexCoord2fARB(Gl.GL_TEXTURE3_ARB, u, v);

                    Gl.glVertex3d(mapx * xscale + step * xscale, mapy * yscale, heightmap[mapx + step, mapy]);
                }
                Gl.glEnd();
            }
        }

        
        /// <summary>
        /// gets normal for vertex at mapx, mapy 
        /// </summary>
        public Vector3 GetNormal(int mapx, int mapy)
        {
            Vector3 total = normalsperquad[mapx, mapy];
            int numnormalsinaverage = 1;
            if (mapx > 0)
            {
                total += normalsperquad[mapx - 1, mapy];
                numnormalsinaverage++;
                if (mapy > 0)
                {
                    total += normalsperquad[mapx, mapy - 1];
                    numnormalsinaverage++;

                    total += normalsperquad[mapx - 1, mapy - 1];
                    numnormalsinaverage++;
                }
            }
            else if (mapy > 0)
            {
                total += normalsperquad[mapx, mapy - 1];
                numnormalsinaverage++;
            }
            return total / numnormalsinaverage;
        }
    }
}

