//Copyright © Autodesk, Inc. 2012. All rights reserved.
//
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//
//http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SharpGL;

using Autodesk.Revit.DB;
using Dynamo.Elements;

namespace dynPSView
{
    public partial class FormDynPSView : Form
    {

        private ParticleSystem mParticleSystem;

        public FormDynPSView()
        {
            InitializeComponent();
            mParticleSystem = new ParticleSystem();
            InitParticleSystem();
        }

        //public void InitParticleSystem()
        //{
        //    Particle a = mParticleSystem.makeParticle(1, new XYZ(), true);
        //    Particle b = mParticleSystem.makeParticle(1, new XYZ(1,0,0), false);
        //    mParticleSystem.makeSpring(a, b, 1, 100, 0.1);
        //}

        public void InitParticleSystem()
        {
            double stepSize = .1;
            int maxPart = 20;
            for (int i = 0; i < maxPart; i++)
            {
                if (i == 0)
                {
                    Particle a = mParticleSystem.makeParticle(1, new XYZ(0, 0, 0), true);
                }
                else
                {
                    Particle b = mParticleSystem.makeParticle(1, new XYZ(i * stepSize, 0, 0), false);
                    mParticleSystem.makeSpring(mParticleSystem.getParticle(i - 1), b, .1, 500, 0.1);
                }
                if (i == maxPart - 1)
                {
                    mParticleSystem.getParticle(i).makeFixed();
                }


            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("Welcome to dynPSView!  We hope you have a wonderful day.");
        }

        private void openGLControl1_OpenGLDraw(object sender, PaintEventArgs e)
        {

            // Step the particle system
            for (int i = 0; i < 200; i++ )
                mParticleSystem.step(0.0002);

            //  Get the OpenGL object, just to clean up the code.
            OpenGL gl = this.openGLControl1.OpenGL;

            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);	// Clear The Screen And The Depth Buffer
            gl.LoadIdentity();					// Reset The View

            gl.Translate(0f, 0.0f, -6.0f);				// Move Left And Into The Screen
            gl.Rotate(-90f, 1f, 0f, 0f);

            gl.Disable(OpenGL.GL_LIGHTING);
            gl.PolygonMode(OpenGL.GL_FRONT, OpenGL.GL_FILL);

            // Draw particles

                for (int i = 0; i < mParticleSystem.numberOfParticles(); i++)
                {
                    Particle p = mParticleSystem.getParticle(i);
                    XYZ a = p.getPosition();
                   
                    gl.Color(1.0f, 1.0f, 1.0f);
                    
                    if ( !p.isFree() )
                        gl.PointSize(4.0f);
                    else
                        gl.PointSize(2.0f);

                    gl.Begin(SharpGL.Enumerations.BeginMode.Points);
                    gl.Vertex(a.X, a.Y, a.Z);
                    gl.End();

                }


            // Draw springs

                for (int i = 0; i < mParticleSystem.numberOfSprings(); i++)
                {

                    ParticleSpring p = mParticleSystem.getSpring(i);
                    XYZ a = p.getOneEnd().getPosition();
                    XYZ b = p.getTheOtherEnd().getPosition();

                    gl.Color(0.8f, 0.8f, 0.8f);

                    gl.Begin(SharpGL.Enumerations.BeginMode.Lines);
                    gl.Vertex(a.X, a.Y, a.Z);
                    gl.Vertex(b.X, b.Y, b.Z);
                    gl.End();

                }

                //gl.Begin(OpenGL.GL_TRIANGLES);					// Start Drawing The Pyramid

                //gl.Color(1.0f, 0.0f, 0.0f);			// Red
                //gl.Vertex(0.0f, 1.0f, 0.0f);			// Top Of Triangle (Front)
                //gl.Color(0.0f, 1.0f, 0.0f);			// Green
                //gl.Vertex(-1.0f, -1.0f, 1.0f);			// Left Of Triangle (Front)
                //gl.Color(0.0f, 0.0f, 1.0f);			// Blue
                //gl.Vertex(1.0f, -1.0f, 1.0f);			// Right Of Triangle (Front)

                //gl.Color(1.0f, 0.0f, 0.0f);			// Red
                //gl.Vertex(0.0f, 1.0f, 0.0f);			// Top Of Triangle (Right)
                //gl.Color(0.0f, 0.0f, 1.0f);			// Blue
                //gl.Vertex(1.0f, -1.0f, 1.0f);			// Left Of Triangle (Right)
                //gl.Color(0.0f, 1.0f, 0.0f);			// Green
                //gl.Vertex(1.0f, -1.0f, -1.0f);			// Right Of Triangle (Right)

                //gl.Color(1.0f, 0.0f, 0.0f);			// Red
                //gl.Vertex(0.0f, 1.0f, 0.0f);			// Top Of Triangle (Back)
                //gl.Color(0.0f, 1.0f, 0.0f);			// Green
                //gl.Vertex(1.0f, -1.0f, -1.0f);			// Left Of Triangle (Back)
                //gl.Color(0.0f, 0.0f, 1.0f);			// Blue
                //gl.Vertex(-1.0f, -1.0f, -1.0f);			// Right Of Triangle (Back)

                //gl.Color(1.0f, 0.0f, 0.0f);			// Red
                //gl.Vertex(0.0f, 1.0f, 0.0f);			// Top Of Triangle (Left)
                //gl.Color(0.0f, 0.0f, 1.0f);			// Blue
                //gl.Vertex(-1.0f, -1.0f, -1.0f);			// Left Of Triangle (Left)
                //gl.Color(0.0f, 1.0f, 0.0f);			// Green
                //gl.Vertex(-1.0f, -1.0f, 1.0f);			// Right Of Triangle (Left)
                //gl.End();						// Done Drawing The Pyramid

                //gl.LoadIdentity();
                //gl.Translate(1.5f, 0.0f, -7.0f);				// Move Right And Into The Screen



            gl.Flush();


        }


        private void openGLControl1_Load(object sender, EventArgs e)
        {

        }
    }
}