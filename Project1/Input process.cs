using System;
using System.IO;
using System.Text;

namespace c__Project_2
{
    internal class RSATeamProject
    {
        //Bien toan cuc UsernameMaHoa, PasswordMaHoa, PhonenumberMaHoa
        static string UsernameMaHoa, PasswordMaHoa, PhonenumberMaHoa;

        //Luu Input tu thong tin nguoi nhap va luu vao mot mang mot chieu co chieu dai 3
        //Luu cac thong tin Username, Password, Phonenumber vao vi tri index 0 1 2 cua mang mot chieu
        static string[] InputUser()
        {
            //cho phep nguoi dung nhap tieng viet
            Console.OutputEncoding = Encoding.UTF8;

            // Khởi tạo các biến
            string username = "", password = "", phonenumber = "";
            string[] OutputUser = new string[3]; //tao mang luu thong tin nguoi dung co chieu dai 3
            bool testusername = false, testpassword = false, testphonenumber = false;

            // Đăng kí username

            while (!testusername)
            {
                // Nhập username
                Console.ForegroundColor = ConsoleColor.Blue;
                Print("Nhập username: ");
                Console.ForegroundColor = ConsoleColor.White;
                username = Console.ReadLine();

                // Kiểm tra khoảng trắng trong username

                if (username.Contains(' '))
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Print("Username không được chứa khoảng trắng. Hãy chờ 2s và nhập lại. Xin cảm ơn!\n");
                    Thread.Sleep(3000); Console.Clear(); continue;
                }

                // Kiểm tra độ dài username neu nho hon 8

                if (username.Length < 8)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Print("Username phải có ít nhất 8 ký tự. Hãy chờ 2s và nhập lại. Xin cảm ơn!\n");
                    Thread.Sleep(3000); Console.Clear(); continue;
                }

                // Kiểm tra độ dài username neu tren 30
                if (username.Length > 30)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Print("Username phải không quá 30 ký tự. Hãy chờ 2s và nhập lại. Xin cảm ơn!\n");
                    Thread.Sleep(3000); Console.Clear(); continue;
                }

                // Kiểm tra ký tự không nằm trong bảng mã tiếng việt không dấu
                bool isAscii = true;

                foreach (char c in username)
                {
                    if (c < 21 || c > 126)
                    {
                        isAscii = false; break;
                    }
                }

                // Hiển thị kết quả
                if (!isAscii)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Print("Username phải là các ký tự không dấu. Hãy chờ 2s và nhập lại. Xin cảm ơn!\n");
                    Thread.Sleep(3000); Console.Clear(); continue;
                }

                testusername = true;
            }

            // Đăng kí password

            while (!testpassword)
            {
                // Nhập password
                Console.ForegroundColor = ConsoleColor.Blue;
                Print("Nhập password: ");
                Console.ForegroundColor = ConsoleColor.White;
                password = Console.ReadLine();

                // Kiểm tra khoảng trắng trong password

                if (password.Contains(' '))
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Print("Password không được chứa khoảng trắng. Hãy chờ 2s và nhập lại. Xin cảm ơn!\n");

                    Thread.Sleep(3000); Console.Clear();

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Print($"Nhập username: ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Print($"{username}\n");
                    continue;
                }

                // Kiểm tra độ dài password nho hon 8

                if (password.Length < 8)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Print("Password phải có ít nhất 8 ký tự. Hãy chờ 2s và nhập lại. Xin cảm ơn!\n");

                    Thread.Sleep(3000); Console.Clear();

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Print($"Nhập username: ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Print($"{username}\n");
                    continue;
                }

                // Kiểm tra độ dài password tren 30
                if (password.Length > 30)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Print("Password phải không quá 30 ký tự. Hãy chờ 2s và nhập lại. Xin cảm ơn!\n");

                    Thread.Sleep(3000); Console.Clear();

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Print($"Nhập username: ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Print($"{username}\n");
                    continue;
                }

                // Kiểm tra ký tự không nằm trong bảng mã tiếng việt không dấu
                bool isAscii = true;

                foreach (char c in password)
                {
                    if (c < 21 || c > 126)
                    {
                        isAscii = false; break;
                    }
                }

                // Hiển thị kết quả

                if (!isAscii)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Print("Password phải là các ký tự không dấu. Hãy chờ 2s và nhập lại. Xin cảm ơn!\n");

                    Thread.Sleep(3000); Console.Clear();

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Print($"Nhập username: ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Print($"{username}\n");
                    continue;
                }

                // Kiểm tra password ít nhất là có một chữ số
                bool hasDigit = false;

                foreach (char c in password)
                {
                    if (Char.IsDigit(c))
                    {
                        hasDigit = true; break;
                    }
                }

                // Hiển thị kết quả

                if (!hasDigit)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Print("Password phải có ít nhất một chữ số. Hãy chờ 2s và nhập lại. Xin cảm ơn!\n");

                    Thread.Sleep(3000); Console.Clear();

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Print($"Nhập username: ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Print($"{username}\n");
                    continue;
                }

                // Kiểm tra password có ít nhất một ký tự đặc biệt hay không
                bool hasSpecialCharacter = false;

                foreach (char c in password)
                {
                    // Kiểm tra ký tự không phải là chữ cái, số hoặc khoảng trắng

                    if (!Char.IsLetterOrDigit(c) && c != ' ')
                    {
                        hasSpecialCharacter = true; break;
                    }
                }

                // Hiển thị kết quả

                if (!hasSpecialCharacter)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Print("Password phải có ít nhất một ký tự đặc biệt. Hãy chờ 2s và nhập lại. Xin cảm ơn!\n");

                    Thread.Sleep(3000); Console.Clear();

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Print($"Nhập username: ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Print($"{username}\n");
                    continue;
                }

                testpassword = true;
            }

            // Đăng kí số điện thoại

            while (!testphonenumber)
            {
                // Nhập số điện thoại
                Console.ForegroundColor = ConsoleColor.Blue;
                Print("Nhập số điện thoại: ");
                Console.ForegroundColor = ConsoleColor.White;
                phonenumber = Console.ReadLine();

                // Kiểm tra số điện thoại xem đúng định dạng 10 số chưa
                int test = phonenumber.Length;

                if (test != 10)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Print("Số điện thoại phải đúng định dạng 10 ký tự số. Hãy chờ 2s và nhập lại. Xin cảm ơn!\n");

                    Thread.Sleep(3000); Console.Clear();

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Print($"Nhập username: ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Print($"{username}\n");

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Print($"Nhập password: ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Print($"{password}\n");
                    continue;
                }

                bool isAllDigit = true;
                // Kiểm tra xem có phải ký tự số

                foreach (char c in phonenumber)
                {
                    if (!Char.IsDigit(c))
                    {
                        isAllDigit = false; break;
                    }
                }

                if (!isAllDigit)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Print("Số điện thoại phải đúng định dạng 10 ký tự số. Hãy chờ 2s và nhập lại. Xin cảm ơn!\n");

                    Thread.Sleep(3000); Console.Clear();

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Print($"Nhập username: ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Print($"{username}\n");

                    Console.ForegroundColor = ConsoleColor.Blue;
                    Print($"Nhập password: ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Print($"{password}\n");
                    continue;
                }

                testphonenumber = true;
            }

            // Thông báo dang nhap thanh cong

            if (testpassword == true && testusername == true)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Print("Tạo tài khoản thành công!\n");
            }

            //gan cac thong tin nguoi dung vao mang OutputUser tai vi tri 0 1 2
            OutputUser[0] = username;
            OutputUser[1] = password;
            OutputUser[2] = phonenumber;

            //ham tra ve gia tri mang chua cac thong tin nguoi dung
            return OutputUser;
        }

        static void Print(string text)
        {
            Console.Write(text);
        }
    }
}