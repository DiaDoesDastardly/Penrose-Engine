using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace PenroseEngine{
    
    public class rendererPipeline
    {
        public static Main(string args[]){
            Console.WriteLine ("Hello World");
        }
        public static int xSize = 400;
        public static int ySize = 400;
        public static double[,] rotationMatrixGenerator(double theta, double phi){
            //Converting input degrees into radians
            theta = (theta/180)*Math.PI;
            phi = (phi/180)*Math.PI;
            //Make rotational matrx 
            double[,] rotationalMatrix = new double[,]{
                {Math.Cos(theta), 0 , -Math.Sin(theta)},
                {-Math.Sin(theta)*Math.Sin(phi), Math.Cos(phi),-Math.Cos(theta)*Math.Sin(phi)},
                {Math.Sin(theta)*Math.Cos(phi), Math.Sin(phi),Math.Cos(theta)*Math.Cos(phi)}
            }; 

            return rotationalMatrix;
        }
        public static double[] rotatePoints(double[,] rotationalMatrix, double[] pointA, double[] offset){
            //Move point by offset 
            pointA = new double[]{
                pointA[0]-offset[0],
                pointA[1]-offset[1],
                pointA[2]-offset[2]
            };
            //Multiply rotated point
            double[] outputPoint = new double[]{
                rotationalMatrix[0,0]*pointA[0]+rotationalMatrix[0,1]*pointA[1]+rotationalMatrix[0,2]*pointA[2],
                rotationalMatrix[1,0]*pointA[0]+rotationalMatrix[1,1]*pointA[1]+rotationalMatrix[1,2]*pointA[2],
                rotationalMatrix[2,0]*pointA[0]+rotationalMatrix[2,1]*pointA[1]+rotationalMatrix[2,2]*pointA[2],
            };
            return outputPoint;
        }
        public static double[][][] rotateTriangles(double[,] rotationalMatrix, gameObject renderableObject,double scale){
            double[][] vertexHolder = new double[renderableObject.vertices.Length][];
            for(int index = 0; index < vertexHolder.Length; index++){
                vertexHolder[index] = new double[3];
            }
            //int[][] triangleHolder = triangleList;
            //Each pixel has the info of {depth, interacted, color}
            //interacted values are 
            //  0.0 == not interacted
            //  1.0 == interacted
            double[][][] screenInfo = new double[xSize][][];
            for(int z = 0; z < screenInfo.Length; z++){
                screenInfo[z] = new double[ySize][];
                for(int x = 0; x < screenInfo[z].Length; x++){
                    screenInfo[z][x] = new double[3];
                }
            }
            double[] tempPoint = new double[]{0.0,0.0,0.0};
            double[] deltaAB = new double[]{0.0,0.0,0.0};
            double[] deltaAC = new double[]{0.0,0.0,0.0};
            double[] targetPoint = new double[]{0.0,0.0,0.0};
            double distAB = 0.0;
            double distAC = 0.0;
            //Rotating all of the points of the object by the rotational matrix
            //For now the offset will be {0,0,0}

            for(int index = 0; index< vertices.GetLength(0); index++){
                tempPoint = rotatePoints(
                    rotationalMatrix, 
                    new double[]{
                        renderableObject.vertices[index][0],
                        renderableObject.vertices[index][1],
                        renderableObject.vertices[index][2]
                    }, 
                    new double[]{0,0,0}
                    );
                vertexHolder[index][0] = tempPoint[0]*scale;
                vertexHolder[index][1] = tempPoint[1]*scale;
                vertexHolder[index][2] = tempPoint[2];
            }

            //Rendering the rotated triangles into the screen data 
            for(int index = 0; index < triangleList.Length; index++){
                //Finding the deltaAB and deltaAC for this triangle 
                deltaAB = new double[]{
                    vertexHolder[renderableObject.triangles[index][1]][0]-vertexHolder[renderableObject.triangles[index][0]][0],
                    vertexHolder[renderableObject.triangles[index][1]][1]-vertexHolder[renderableObject.triangles[index][0]][1],
                    vertexHolder[renderableObject.triangles[index][1]][2]-vertexHolder[renderableObject.triangles[index][0]][2],
                };
                deltaAC = new double[]{
                    vertexHolder[renderableObject.triangles[index][2]][0]-vertexHolder[renderableObject.triangles[index][0]][0],
                    vertexHolder[renderableObject.triangles[index][2]][1]-vertexHolder[renderableObject.triangles[index][0]][1],
                    vertexHolder[renderableObject.triangles[index][2]][2]-vertexHolder[renderableObject.triangles[index][0]][2],
                };
                //Doing backface culling at this step
                if(deltaAB[0]*deltaAC[1] - deltaAC[0]*deltaAB[1] < 0){
                    continue;
                }
                //Finding the distance from point A and point B as well as from point A and point C
                distAB = Math.Sqrt(
                    deltaAB[0]*deltaAB[0]+
                    deltaAB[1]*deltaAB[1]+
                    deltaAB[2]*deltaAB[2]
                );
                distAC = Math.Sqrt(
                    deltaAC[0]*deltaAC[0]+
                    deltaAC[1]*deltaAC[1]+
                    deltaAC[2]*deltaAC[2]
                );
                //Going through all of the pixels in the triangle (using the i+j <= 1 rule)
                for(double i = 0; i <= 1; i += 1/distAB){
                    for(double j = 0; j+i <= 1; j += 1/distAC){
                        //Finding the point in 3d space
                        targetPoint = new double[]{
                            1*(vertexHolder[renderableObject.triangles[index][0]][0]+xSize/2)+i*deltaAB[0]+j*deltaAC[0],
                            1*(vertexHolder[renderableObject.triangles[index][0]][1]+ySize/2)+i*deltaAB[1]+j*deltaAC[1],
                            1*vertexHolder[renderableObject.triangles[index][0]][2]+i*deltaAB[2]+j*deltaAC[2]
                        };
                        //Making sure the point is on the screen
                        if(targetPoint[0]>0 && xSize>targetPoint[0] && targetPoint[1]>0 && ySize>targetPoint[1]){
                            //Seeing if this pixel has been interacted and if depth is lower than current value
                            if(
                                screenInfo[(int)targetPoint[0]][(int)targetPoint[1]][1] == 0.0f || 
                                screenInfo[(int)targetPoint[0]][(int)targetPoint[1]][0] < targetPoint[2]
                            ){
                                //Setting the pixel to interacted and the depth value to the targetPoint's 
                                screenInfo[(int)targetPoint[0]][(int)targetPoint[1]][2] = triangleColor[index];
                                screenInfo[(int)targetPoint[0]][(int)targetPoint[1]][1] = 1.0f;
                                screenInfo[(int)targetPoint[0]][(int)targetPoint[1]][0] = targetPoint[2];
                            }
                        }
                    }
                }
            }
            //returning the screen data
            return screenInfo;
        } 
        public static Image renderToScreen(double[][][] screenInfo){
            Bitmap screenImage = new Bitmap(screenInfo.Length, screenInfo[0].Length);
            //Creating the tempColor holder that will be used to color the triangles
            Color tempColor = Color.FromArgb(255,0,0,0);
            for(int x = 0; x< screenInfo.Length; x++){
                //screenOutput[x] = new int[screenInfo[x].Length][];
                for(int y = 0; y< screenInfo[0].Length; y++){
                    //screenOutput[x][y] = new int[screenInfo[x][y].Length];
                    if(screenInfo[x][y][1] == 1.0f){
                        //Pulling the color from screenData
                        tempColor = Color.FromArgb(255,(int)screenInfo[x][y][2],(int)screenInfo[x][y][2],(int)screenInfo[x][y][2]);
                        screenImage.SetPixel(x,y,tempColor);
                    }else{
                        //If the pixel has not been interacted with, then set color to white
                        tempColor = Color.FromArgb(255,255,255,255);
                        screenImage.SetPixel(x,y,tempColor);
                    }
                }
            }
            Image output = (Image)screenImage;
            return output;
        }
	}
    public class gameObject(){
        string name;
        vector3[] vertices;
        int[][] triangles;
        int[] triangleColors;

        vector3 position;

        public gameObject(){
            //Empty Case
        }
        public gameObject(string filePath){
            //If the file type is not obj then throw an exception
            if(filePath[filePath.Length-3]+filePath[filePath.Length-2]+filePath[filePath.Length-1] != "obj"){
                throw new Exception("Cannot import: file format "+filePath[filePath.Length-3]+filePath[filePath.Length-2]+filePath[filePath.Length-1]+" is not supported ");
            }
            //Getting the contents of the obj file into a line by line format
            string[] fileContents = File.ReadAllText(filePath).split("\n");
            //Creating lists to hold the triangles and vertices we find
            List<vector3> foundVertices = new List<vector3>();
            List<int[]> foundTriangles = new List<int[]>();
            //Creating array that will hold the contents of a line when we remove all of the spaces
            string[] splitLineContents;
            //Checking each line for which starting characters they have and acting accordingly 
            for(int index = 0; index<fileContents.Length; index++){
                //We check the first two characters to make sure we don't misread anything
                if(fileContents[index][0]+fileContents[index][1]=="o "){
                    name = fileContents[index].split(" ")[1];
                }
                if(fileContents[index][0]+fileContents[index][1]=="v "){
                    splitLineContents = fileContents[index].split(" ");
                    foundVertices.Add(new vector3(
                        Convert.ToDouble(splitLineContents[1]),
                        Convert.ToDouble(splitLineContents[2]),
                        Convert.ToDouble(splitLineContents[3]),
                    ));
                }
                if(fileContents[index][0]+fileContents[index][1]=="f "){
                    splitLineContents = fileContents[index].split(" ");
                    foundTriangles.Add(new int[]{
                        Convert.ToDouble(splitLineContents[1].split("/")[1])-1,
                        Convert.ToDouble(splitLineContents[2].split("/")[1])-1,
                        Convert.ToDouble(splitLineContents[3].split("/")[1])-1                        
                    });
                }
            }
        }
    }
    public class component(){
        //Component Class for future ECS system
    }
    public class vector3(){
        public double x;
        public double y;
        public double z;
        public vector3(){
            //Empty Case
        }
        public vector3(double x, double y, double z){
            x = this.x;
            y = this.y;
            z = this.z;
        }
    }
    public partial class MyForm : Form
    {
        //private rendererPipeline render;
        private Timer timer;
        private int intervalMilliseconds = (int)(1000/60); // Change this to set the interval in milliseconds
        private Random random = new Random();
        private PictureBox pictureBox1;
        private double[,] rotationalMatrix;
        private int[][] triangleList;
        private double[][] vertices;
        private int[] triangleColor;
        private double scale;
        private TrackBar trackBar1;
        private TrackBar trackBar2;

        public MyForm(double[,] rotationalMatrix, int[][] triangleList, double[][] vertices, int[] triangleColor,double scale)
        {
            //render = new rendererPipeline();
            this.rotationalMatrix = rotationalMatrix;
            this.triangleList = triangleList;
            this.vertices = vertices;
            this.triangleColor = triangleColor;
            this.scale = scale;
            //InitializeComponent();
            pictureBox1 = new PictureBox();
            //pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            InitializeComponent();
            InitializeTimer();
        }
        private void InitializeComponent()
        {
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            // Create a new TrackBar control
            trackBar1 = new TrackBar();

            // Set the properties of the TrackBar
            trackBar1.Minimum = -180;
            trackBar1.Maximum = 180;
            trackBar1.Value = 0; // Initial value
            trackBar1.TickStyle = TickStyle.TopLeft;
            trackBar1.TickFrequency = 10;
            trackBar1.Width = 200;
            trackBar1.Location = new System.Drawing.Point(0, 400);

            // Create a new TrackBar control
            trackBar2 = new TrackBar();

            // Set the properties of the TrackBar
            trackBar2.Minimum = -180;
            trackBar2.Maximum = 180;
            trackBar2.Value = 0; // Initial value
            trackBar2.TickStyle = TickStyle.TopLeft;
            trackBar2.TickFrequency = 10;
            trackBar2.Width = 200;
            trackBar2.Location = new System.Drawing.Point(200, 400);

            // Add the TrackBar to the form's Controls collection
            Controls.Add(trackBar1);
            Controls.Add(trackBar2);

            // PictureBox
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(400, 400);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            
            // Form
            this.Controls.Add(this.pictureBox1);
            this.ClientSize = new System.Drawing.Size(400, 440);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
        }

        private void InitializeTimer()
        {
            timer = new Timer();
            timer.Interval = intervalMilliseconds;
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        // Timer tick event handler
        private void Timer_Tick(object sender, EventArgs e)
        {
            // Call the function to generate the bitmap
            //Bitmap originalImage = GenerateBitmap();

            // Call the edit function
            //Bitmap editedImage = EditPicture(originalImage);
            rotationalMatrix = rendererPipeline.rotationMatrixGenerator(trackBar1.Value,trackBar2.Value);
            double[][][] screenInfo = rendererPipeline.rotateTriangles(rotationalMatrix, triangleList, vertices, triangleColor, scale);
            Image frame = rendererPipeline.renderToScreen(screenInfo);

            // Update the PictureBox with the edited image
            pictureBox1.Image = frame;
        }

        // Function to generate the bitmap
        

        // Function to edit the picture
        private Bitmap EditPicture(Bitmap original)
        {
            // Example: Flip the image horizontally
            original.RotateFlip(RotateFlipType.RotateNoneFlipX);
            return original;
        }
    }
}
