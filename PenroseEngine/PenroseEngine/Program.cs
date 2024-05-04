// See https://aka.ms/new-console-template for more information
using PenroseEngine;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        gameObject cube = new gameObject("C:\\Users\\borgs\\Documents\\GitHub\\Penrose-Engine\\PenroseEngine\\PenroseEngine\\cube.obj");
        double[,] rotationMatrix = rendererPipeline.rotationMatrixGenerator(0,0);
        MyForm form = new MyForm(rotationMatrix,cube,100);
        Application.Run(form);
    }
}