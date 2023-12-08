using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading; //enables use of Thread.Sleep() “wait” method
using Thorlabs.MotionControl.DeviceManagerCLI;
using Thorlabs.MotionControl.GenericMotorCLI.Settings; //this will specifically target only the commands contained within the .Settings sub-class library in *.GenericMotorCLI.dll.
using Thorlabs.MotionControl.KCube.DCServoCLI; // ****** VERIFICAR SE ESSA DLL ESTÁ CERTA PARA O DISPOSITIVO QUE ESTAMOS USANDO ******
using System.Security.Cryptography;
using Thorlabs.MotionControl.GenericMotorCLI.AdvancedMotor;
using Thorlabs.MotionControl.GenericMotorCLI;


namespace fastPositionScan
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // We create the serial number string of your connected controller. This will
            // be used as an argument for LoadMotorConfiguration(). You can replace this
            // serial number with the number printed on your device.
            // ****** SUBSTITUIR O NÚMERO DE SÉRIE PELO DO DISPOSITIVO UTILIZADO ******
            string serialNo_ServoY = "27261089";
            string serialNo_ServoX = "27261487";

            // This instructs the DeviceManager to build and maintain the list of
            // devices connected.
            DeviceManagerCLI.BuildDeviceList();
            // This creates an instance of KCubeDCServo class, passing in the Serial Number parameter.
            KCubeDCServo ServoY = KCubeDCServo.CreateKCubeDCServo(serialNo_ServoY);
            KCubeDCServo ServoX = KCubeDCServo.CreateKCubeDCServo(serialNo_ServoX);
            // We tell the user that we are opening connection to the device.
            Console.WriteLine("Opening devices {0} and {1}", serialNo_ServoY, serialNo_ServoX);
            // This connects to the device.
            ServoX.Connect(serialNo_ServoX);
            ServoY.Connect(serialNo_ServoY);
            // Wait for the device settings to initialize. We ask the device to
            // throw an exception if this takes more than 5000ms (5s) to complete.
            ServoX.WaitForSettingsInitialized(5000);
            ServoY.WaitForSettingsInitialized(5000);
            // This calls LoadMotorConfiguration on the device to initialize the DeviceUnitConverter object required for real world unit parameters.
            MotorConfiguration motorSettings_ServoX = ServoX.LoadMotorConfiguration(serialNo_ServoX, DeviceConfiguration.DeviceSettingsUseOptionType.UseFileSettings);
            MotorConfiguration motorSettings_ServoY = ServoY.LoadMotorConfiguration(serialNo_ServoY, DeviceConfiguration.DeviceSettingsUseOptionType.UseFileSettings);
            // This starts polling the device at intervals of 250ms (0.25s).
            ServoX.StartPolling(250);
            ServoY.StartPolling(250);
            // We are now able to Enable the device otherwise any move is ignored. You should see a physical response from your controller.
            ServoX.EnableDevice();
            ServoY.EnableDevice();
            Console.WriteLine("Devices Enabled");
            // Needs a delay to give time for the device to be enabled.
            Thread.Sleep(500);
            // Home the stage/ actuator.
            Console.WriteLine("Actuator is Homing");
            ServoX.Home(60000);
            ServoY.Home(60000);

            //Inicia o QDC
            QDC.Init();

            //INPUT DE DADOS ESCOLHIDOS PELO USUARIO PARA REALIZAR O SCAN
            bool error = true;
            const decimal positionLimit = 30;
            const decimal stepInferiorLimit = 0.001m;
            string input;

            decimal initialPositionX = 0;
            decimal initialPositionY = 0;
            decimal finalPositionX = 0;
            decimal finalPositionY = 0;
            decimal stepX = 0;
            decimal stepY = 0;
            int numStepsX = 0;
            int numStepsY = 0;

            int medicoesCarga = 100;
            int carga = 0;
            decimal PositionX = 0;
            decimal PositionY = 0;

            List<int>vetorCarga = new List<int>();
            List<decimal>vetorPosicaoX = new List<decimal>();
            List<decimal>vetorPosicaoY = new List<decimal>();




            //pede ao usuário a posição inicial de x
            while (error)
            {
                Console.Write("Set X initial position: ");
                input = Console.ReadLine();

                if (decimal.TryParse(input, out initialPositionX))
                {
                    if (initialPositionX < positionLimit) //limite de segurança para posição
                    {
                        error = !error;
                    }
                    else
                    {
                        Console.WriteLine("Entrada inválida. Certifique-se de que o valor de entrada seja menor que o limite de segurança.\nLimite de segurança = " + positionLimit);
                    }

                }
                else
                {
                    Console.WriteLine("Entrada inválida. Certifique-se de digitar um número válido.\n");
                }
            }


            //pede ao usuário a posição inicial de y
            while (!error)
            {
                Console.Write("Set Y initial position: ");
                input = Console.ReadLine();

                if (decimal.TryParse(input, out initialPositionY))
                {
                    if (initialPositionY < positionLimit) //limite de segurança para posição
                    {
                        error = !error;
                    }
                    else
                    {
                        Console.WriteLine("Entrada inválida. Certifique-se de que o valor de entrada seja menor que o limite de segurança.\nLimite de segurança = " + positionLimit);
                    }

                }
                else
                {
                    Console.WriteLine("Entrada inválida. Certifique-se de digitar um número válido.\n");
                }
            }

            //pede ao usuário a posição final de x
            while (error)
            {

                Console.Write("Set X final position: ");
                input = Console.ReadLine();

                if (decimal.TryParse(input, out finalPositionX))
                {
                    if (finalPositionX < positionLimit & finalPositionX > initialPositionX)
                    {
                        error = !error;
                    }
                    else
                    {
                        Console.WriteLine("Entrada inválida. Certifique-se de que o valor de entrada seja menor que o limite de segurança e maior do que o valor de posição inicial.\nLimite de segurança = " + positionLimit);
                    }
                }
                else
                {
                    Console.WriteLine("Entrada invalida. Certifique-se de digitar um numero valido.\n");
                }
            }

            //pede ao usuário a posição final de y
            while (!error)
            {
                Console.Write("Set Y final position: ");
                input = Console.ReadLine();

                if (decimal.TryParse(input, out finalPositionY))
                {
                    if (finalPositionX < positionLimit & finalPositionX > initialPositionX)
                    {
                        error = !error;
                    }
                    else
                    {
                        Console.WriteLine("Entrada inválida. Certifique-se de que o valor de entrada seja menor que o limite de segurança e maior do que o valor de posição inicial.\nLimite de segurança = " + positionLimit);
                    }
                }
                else
                {
                    Console.WriteLine("Entrada invalida. Certifique-se de digitar um numero valido.\n");
                }
            }

            //define o valor do passo em x
            while (error)
            {
                Console.WriteLine("Insira o numero de medicoes que devem ser feitas entre os limites dados (numero de passos) para a direcao x: ");
                input = Console.ReadLine();

                if (int.TryParse(input, out numStepsX))
                {
                    stepX = (finalPositionX - initialPositionX) / numStepsX;

                    if (stepX > stepInferiorLimit)
                    {
                        error = !error;
                    }
                    else
                    {
                        Console.WriteLine("Entrada inválida. Certifique-se de que o valor de entrada seja maior que o limite inferior de valor de passo.\nLimite inferior de valor de passo = " + stepInferiorLimit);
                    }
                }
                else
                {
                    Console.WriteLine("Entrada invalida. Certifique-se de digitar um numero valido.\n");
                }
            }

            //define o valor do passo em y
            while (!error)
            {
                Console.WriteLine("Insira o numero de medicoes que devem ser feitas entre os limites dados (numero de passos) para a direcao y: ");
                input = Console.ReadLine();

                if (int.TryParse(input, out numStepsY))
                {
                    stepY = (finalPositionY - initialPositionY) / numStepsY;

                    if (stepY > stepInferiorLimit)
                    {
                        error = !error;
                    }
                    else
                    {
                        Console.WriteLine("Entrada inválida. Certifique-se de que o valor de entrada seja maior que o limite inferior de valor de passo.\nLimite inferior de valor de passo = " + stepInferiorLimit);
                    }
                }
                else
                {
                    Console.WriteLine("Entrada invalida. Certifique-se de digitar um numero valido.\n");
                }
            }


            //LOOP PARA REALIZARO SCAN

            //Move os servos para a posição inicial
            Console.WriteLine("Moving to initial position...");
            ServoX.MoveTo(initialPositionX, 60000);
            ServoY.MoveTo(initialPositionY, 60000);

            //Move relativo a posição inicial
            Console.WriteLine("Scan in execution...");

            for (int i = 0; i < numStepsY + 1; i++)
            {
                PositionY = i * stepY;

                for (int j = 0; j < numStepsX; j++)
                {
                    carga = QDC.Read(medicoesCarga);
                    PositionX = j* stepX;

                    vetorCarga.Add(carga);
                    vetorPosicaoX.Add(PositionX);
                    vetorPosicaoY.Add(PositionY);

                    Console.WriteLine("Carga: " + carga);
                    Console.WriteLine("Posicao X: " + PositionX);
                    Console.WriteLine("Posicao Y: " + PositionY);

                    ServoX.MoveRelative(MotorDirection.Forward, stepX, 60000);
                }

                ServoX.MoveTo(initialPositionX, 60000);
                ServoY.MoveRelative(MotorDirection.Forward, stepY, 60000);
                
            }


            //ENCERRA O PROGRAMA
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

            // Stop polling the device.
            ServoY.StopPolling();
            ServoX.StopPolling();
            // This shuts down the controller. This will use the Disconnect() function to close communications &will then close the used library.
            ServoY.ShutDown();
            ServoX.ShutDown();

            //encerra o QDC
            QDC.End();
        }
    }
}
