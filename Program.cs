using System;


namespace CirculoConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var controller = new WorldController();
            var object1 = WorldController.Generator(controller);
            var object2 = WorldController.Generator(controller);

            GameObjectTask.Start();

            while (true) {

                GameObjectTask.Update();
                
                // nを入力で終了
                Console.Write("Continue? (n/Y): ");
                var input = Console.ReadLine();
                if (input.Equals("n")) {break;}
            }
        }
    }
}
