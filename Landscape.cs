using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Toolkit;

namespace Project1
{
    using SharpDX.Toolkit.Graphics;
    using SharpDX.Toolkit.Input;

    class Landscape : ColoredGameObject
    { 
        // The height map we are going to generate
        private float[,] heightMap;
        
        // Number of iterations to perform when generating the terrain
        private static int interations = 4;

        // Allows us to easily change size of the map
        private float xscale = 4;
        private float yscale = 4;
        private float zscale = 20;

        // Initial change rate
        private float initialFactor = 5.0f;

        // The smoothing factor
        private float smoothingFactor = 0.2f;

        // The height of the player
        private float playerHeight = 2.0f;

        // Height of the 4 corners
        private float cornerHeight = -0.5f;

        // Height of the water
        private float waterHeight = -0.25f;

        // The alpha to render the water at
        private Color waterColor = new Color(0, 0, 255, 100);

        // Camera rotation
        private float rotX = 1.0f;
        private float rotY = -1f;
        private float rotZ = 0;

        // The camera's position
        Vector3 pos;

        // How fast we move
        private float moveSpeed = 0.006f;

        public Landscape(Project1Game game)
        {
            // Setup the diamond square stuff
            DiamondSquare();

            // Calculate the size of our terrain
            int terrainWidth = getTerrainWidth();

            // Workout how many triangles we need
            int totalVertexes = (terrainWidth - 1) * (terrainWidth - 1) * 2 * 3;

            // Add vertexes for the water
            totalVertexes += 2 * 3;

            // Build the mesh
            VertexPositionNormalColor[] vertexData = new VertexPositionNormalColor[totalVertexes];

            int triangleCount = 0;

            // Populate triangles
            for (int x = 0; x < terrainWidth - 1; x++)
            {
                for (int y = 0; y < terrainWidth - 1; y++)
                {
                    // First Triangle
                    Vector3 x1 = new Vector3(x * xscale, y * yscale, zscale * heightMap[x, y]);
                    Vector3 x2 = new Vector3(x * xscale, y * yscale + yscale, zscale * heightMap[x, y + 1]);
                    Vector3 x3 = new Vector3(x * xscale + xscale, y * yscale, zscale * heightMap[x + 1, y]);
                    Vector3 xNorm = Vector3.Cross(x1 - x2, x3 - x1);

                    // Second Triangle
                    Vector3 y1 = x2;
                    Vector3 y2 = new Vector3(x * xscale + xscale, y * yscale + yscale, zscale * heightMap[x + 1, y + 1]);
                    Vector3 y3 = new Vector3(x * xscale + xscale, y * yscale, zscale * heightMap[x + 1, y]);
                    Vector3 yNorm = Vector3.Cross(y1 - y2, y3 - y1);

                    // Bottom Left
                    vertexData[triangleCount++] = new VertexPositionNormalColor(x1, xNorm, GetColor(heightMap[x, y]));
                    // Top Left
                    vertexData[triangleCount++] = new VertexPositionNormalColor(x2, xNorm, GetColor(heightMap[x, y + 1]));
                    // Top Right
                    vertexData[triangleCount++] = new VertexPositionNormalColor(x3, xNorm, GetColor(heightMap[x + 1, y]));

                    // Top left
                    vertexData[triangleCount++] = new VertexPositionNormalColor(y1, yNorm, GetColor(heightMap[x, y + 1]));
                    // Top Right
                    vertexData[triangleCount++] = new VertexPositionNormalColor(y2, yNorm, GetColor(heightMap[x + 1, y + 1]));
                    // Bottom Right
                    vertexData[triangleCount++] = new VertexPositionNormalColor(y3, yNorm, GetColor(heightMap[x + 1, y]));
                }
            }

            Vector3 waterNorm = new Vector3(0, 0, 1);

            // Add water
            vertexData[triangleCount++] = new VertexPositionNormalColor(new Vector3(0, 0, waterHeight * zscale), waterNorm, waterColor);
            vertexData[triangleCount++] = new VertexPositionNormalColor(new Vector3(0, terrainWidth * yscale, waterHeight * zscale), waterNorm, waterColor);
            vertexData[triangleCount++] = new VertexPositionNormalColor(new Vector3(terrainWidth * xscale, 0, waterHeight * zscale), waterNorm, waterColor);

            vertexData[triangleCount++] = new VertexPositionNormalColor(new Vector3(0, terrainWidth * yscale, waterHeight * zscale), waterNorm, waterColor);
            vertexData[triangleCount++] = new VertexPositionNormalColor(new Vector3(terrainWidth * xscale, terrainWidth * yscale, waterHeight * zscale), waterNorm, waterColor);
            vertexData[triangleCount++] = new VertexPositionNormalColor(new Vector3(terrainWidth * xscale, 0, waterHeight * zscale), waterNorm, waterColor);

            // Default position
            pos = new Vector3(0, 0, -34);

            vertices = Buffer.Vertex.New(
                game.GraphicsDevice,
                vertexData
            );

            basicEffect = new BasicEffect(game.GraphicsDevice)
            {
                VertexColorEnabled = true,
                View = Matrix.LookAtLH(new Vector3(0, 0, -5), new Vector3(0, 0, 0), Vector3.UnitY),
                Projection = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, (float)game.GraphicsDevice.BackBuffer.Width / game.GraphicsDevice.BackBuffer.Height, 0.1f, 100.0f),
                World = Matrix.Identity
            };

            // Setup lighting
            basicEffect.LightingEnabled = true;
            basicEffect.AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f);
            basicEffect.DirectionalLight0.DiffuseColor = new Vector3(0.5f, 0.5f, 0.5f);
            basicEffect.DirectionalLight0.SpecularColor = new Vector3(0.75f, 0.75f, 0.75f);

            inputLayout = VertexInputLayout.FromBuffer(0, vertices);
            this.game = game;
        }

        // Returns how big our heightMap array will be
        private int getTerrainWidth()
        {
            return (int)Math.Pow(2.00, (double)interations) + 1;
        }

        // Performs the diamond square algorithum
        private void DiamondSquare()
        {
            // Calculate the size of our terrain
            int terrainWidth = getTerrainWidth();

            // Allocate the height map array
            heightMap = new float[terrainWidth, terrainWidth];

            // Set initial heights
            heightMap[0, 0] = cornerHeight;
            heightMap[terrainWidth - 1, 0] = cornerHeight;
            heightMap[terrainWidth - 1, terrainWidth - 1] = cornerHeight;
            heightMap[0, terrainWidth - 1] = cornerHeight;

            // Perform the squaring
            Square(terrainWidth / 2, terrainWidth / 2, terrainWidth / 2, initialFactor * smoothingFactor);
        }

        // Performs a square, 4 diamonds, then 4 squares
        private void Square(int x, int y, int size, float factor)
        {
            if (size < 1) return;

            // Grab the 4 corner values
            float topLeft = heightMap[x - size, y - size];
            float topRight = heightMap[x + size, y - size];
            float bottomRight = heightMap[x + size, y + size];
            float bottomLeft = heightMap[x - size, y + size];

            // Calculate this point
            heightMap[x, y] = (topLeft + topRight + bottomRight + bottomLeft)/4 + Project1Game.RandomFloat(-factor, factor);

            // Calculate diamonds
            Diamond(x - size, y, size, factor);
            Diamond(x, y - size, size, factor);
            Diamond(x + size, y, size, factor);
            Diamond(x, y + size, size, factor);

            // Square the remaining sides
            Square(x - size / 2, y - size / 2, size / 2, factor * smoothingFactor);
            Square(x + size / 2, y - size / 2, size / 2, factor * smoothingFactor);
            Square(x - size / 2, y + size / 2, size / 2, factor * smoothingFactor);
            Square(x + size / 2, y + size / 2, size / 2, factor * smoothingFactor);

        }

        // Performs a diamond
        private void Diamond(int x, int y, int size, float factor)
        {
            // Grab the coodinates of the diamond points
            int fl = x - size;  // Left
            int ft = y - size;  // Top
            int fr = x + size;  // Right
            int fb = y + size;  // Bottom

            // Workout how far to offset if we overflow
            int offsetFactor = getTerrainWidth()-1;

            // Check if we need to wrap
            if (fl < 0) fl += offsetFactor;
            if (ft < 0) ft += offsetFactor;
            if (fr > offsetFactor) fr -= offsetFactor;
            if (fb > offsetFactor) fb -= offsetFactor;

            // Grab diamond corner values
            float top = heightMap[x, ft];
            float right = heightMap[fl, y];
            float bot = heightMap[x, fb];
            float left = heightMap[fl, y];

            // Calculate this point
            heightMap[x, y] = (top + right + bot + left) / 4 + Project1Game.RandomFloat(-factor, factor);
        }

        // Calculates the colour terrain should be at a given height
        private Color GetColor(float height)
        {
            if (height < -0.95)
            {
                int c = (int)((height + 0.95) / 1 * 20) + 235;
                return new Color(c, c, c, 255);
            }
            if (height < -0.25) return new Color(0, (int)(height / -1.5 * 155) + 100, 0, 255);

            if (height < 0.5) return new Color((int)((height + 0.25)/0.75 * 100+50), 60, 0, 255);
            
            return Color.Brown;
        }

        // Returns the height of the terrain at a given point
        private float GetMinHeight()
        {
            // Calculate the size of our terrain
            int terrainWidth = getTerrainWidth();

            float xCoord = pos.X / xscale;
            float yCoord = pos.Y / yscale;

            // Check if we are even on the terrain
            if (xCoord >= 0 && xCoord < terrainWidth - 1 && yCoord >= 0 && yCoord < terrainWidth - 1)
            {
                // Calculate which section of the heightMap we should look at
                int xLow = (int)xCoord;
                int yLow = (int)yCoord;

                // Grab the three useful coordinates
                float baseHeight = heightMap[xLow, yLow];
                float xHeight = heightMap[xLow + 1, yLow];
                float yHeight = heightMap[xLow, yLow + 1];

                // Return the heightest point between the coordinates
                return Math.Min(
                    (xCoord - xLow) * xHeight + (1 - (xCoord - xLow)) * baseHeight,
                    (yCoord - yLow) * yHeight + (1 - (yCoord - yLow)) * baseHeight
                );
            }

            return -1;
        }

        public override void Update(GameTime gameTime)
        {
            // Rotate the cube.
            var time = (float)gameTime.TotalGameTime.TotalSeconds;

            // Workout how much the mouse position has changed
            float xDif = game.mouseCenter.X - game.mouseState.X;
            float yDif = game.mouseCenter.Y - game.mouseState.Y;

            // Rotate camera
            rotY += xDif;
            rotX += yDif;


            // Moving Forwards
            int forwards = 0;
            if (game.keyboardState.IsKeyDown(Keys.W)) { forwards--; }
            if (game.keyboardState.IsKeyDown(Keys.S)) { forwards++; }

            if (forwards != 0)
            {
                pos.X += (float)Math.Sin(rotY) * moveSpeed * gameTime.ElapsedGameTime.Milliseconds * forwards;
                pos.Y -= (float)Math.Cos(rotY) * moveSpeed * gameTime.ElapsedGameTime.Milliseconds * forwards;
            }

            // Strafing
            int sideWays = 0;
            if (game.keyboardState.IsKeyDown(Keys.A)) { sideWays--; }
            if (game.keyboardState.IsKeyDown(Keys.D)) { sideWays++; }

            if (sideWays != 0)
            {
                pos.X += (float)Math.Cos(rotY) * moveSpeed * gameTime.ElapsedGameTime.Milliseconds * sideWays;
                pos.Y += (float)Math.Sin(rotY) * moveSpeed * gameTime.ElapsedGameTime.Milliseconds * sideWays;
            }

            // Up and Down
            int upDown = 0;
            if (game.keyboardState.IsKeyDown(Keys.Q)) { upDown--; }
            if (game.keyboardState.IsKeyDown(Keys.Z)) { upDown++; }

            if (upDown != 0)
            {
                pos.Z += moveSpeed * gameTime.ElapsedGameTime.Milliseconds * upDown;
            }

            // Check height
            float minHeight = GetMinHeight() * zscale;
            if (pos.Z > minHeight - playerHeight) pos.Z = minHeight - playerHeight;

            //if (game.keyboardState.IsKeyDown(Keys.W)) { pos.X += 0.006f * gameTime.ElapsedGameTime.Milliseconds; }

            if (game.keyboardState.IsKeyDown(Keys.Z)) { rotZ -= 0.006f * gameTime.ElapsedGameTime.Milliseconds; }
            if (game.keyboardState.IsKeyDown(Keys.X)) { rotX -= 0.006f * gameTime.ElapsedGameTime.Milliseconds; }
            if (game.keyboardState.IsKeyDown(Keys.Y)) { rotY -= 0.006f * gameTime.ElapsedGameTime.Milliseconds; }

            // Move view
            basicEffect.World = Matrix.Translation(-pos);
            basicEffect.Projection = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, (float)game.GraphicsDevice.BackBuffer.Width / game.GraphicsDevice.BackBuffer.Height, 0.1f, 100.0f);

            // Rotate view
            basicEffect.View = Matrix.Identity * Matrix.RotationX(rotX) * Matrix.RotationY(rotY);

            // Update Lighting
            float timeScale = time * 0.25f;
            basicEffect.DirectionalLight0.Direction = new Vector3((float)Math.Sin(timeScale), (float)Math.Sin(timeScale), -(float)Math.Cos(timeScale));
        }

        public override void Draw(GameTime gameTime)
        {
            // Setup the vertices
            game.GraphicsDevice.SetVertexBuffer(vertices);
            game.GraphicsDevice.SetVertexInputLayout(inputLayout);
            game.GraphicsDevice.SetBlendState(game.GraphicsDevice.BlendStates.AlphaBlend);

            // Apply the basic effect technique and draw the rotating cube
            basicEffect.CurrentTechnique.Passes[0].Apply();
            game.GraphicsDevice.Draw(PrimitiveType.TriangleList, vertices.ElementCount);
        }
    }
}
