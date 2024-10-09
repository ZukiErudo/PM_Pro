using System;
using System.Collections.Generic;
using System.Transactions;

namespace C__Project_1
{
    class TaskIDandSubtasksID
    {
        public string TaskID;
        public int latestPositionID = 0; 
        public List<string> SubTaskIDS = new List<string>(); //danh sach id cua cong viec con

        public TaskIDandSubtasksID(string ID)
        {
            TaskID = ID;
        }
    }

    class Task
    {
        public string TaskID;
        public string TaskName;
        public List<string> Desription = new List<string>();
        public string Priority = "";// Cho nguoi dung biet muc do uu tien cua cong viec

        public Task(string taskID, string taskName)
        {
            TaskID = taskID;
            TaskName = taskName;
        }

        public int TaskNodeLevelInTree; //co the dung hoac ko
        public int TaskFloat; //Thoi gian tri hoan tuong ung cong viec (ma khong anh huong len toan bo project)
        
        public DateTime StartDate = DateTime.MinValue; //Ngay bat dau (Min Value chi viec StartDate chua duoc gan gia tri ngay cu the)
        public DateTime EndDate = DateTime.MinValue; //Ngay ket thuc (Min Value chi viec DueDate chua duoc gan gia tri ngay cu the)
        public int Duration = 1; //Thoi han hoan thanh cong viec (mac dinh 1 ngay)
        public int TimeBudget; //In hours; Thoi gian gioi han cong viec duoc phep thuc hien

        public string Status = ""; //Trang thai cua cong viec: chua bat dau -> dang thuc hien -> da hoan thanh hoac da bi huy bo
        public int PercentageCompleted = 0; //(For non-leaf node only?) Tien do hoan thanh cong viec (xac dinh bang phan tram)

        public bool IsLeafNode = true; //Xac dinh xem nut co phai la khong
        public bool OnCriticalPath = false; //Cho biet xem cong viec nao day co duoc phep hoan hay khong

        public List<string> AssignedTeamMembers = new List<string>(); //Danh sach thanh vien duoc phan cong mot cong viec cu the

        public Task? ParentTask; //Con tro chi vao cong viec nam tren nut cha
        public Dictionary<string, Task> SubTasks = new Dictionary<string, Task>(); //Tu dien chua danh sach cong viec con (cua 1 cong viec cu the). Key: ID cong viec; Value: Cong viec (con)
    }

    class TypeLeadLag
    {
        public string Type; // Dang phu thuoc: S - S; F - F; F - S; S - F
        public int Lead = 0; // - Delta phu thuoc cong viec giua 2 cong viec cu the (khac nhau)
        public int Lag = 0; // Delta phu thuoc cong viec giua 2 cong viec cu the (khac nhau)

        public TypeLeadLag(string type)
        {
            Type = type;
        }
    }

    // Summary:
    // Lop luu tru thong tin cua phu thuoc (cua 1 cong viec) bao gom:
    // Ten cong viec; Tu dien chua dang phu thuoc, delta thoi gian chenh lech cua 2 cong viec. Key: Ten Cong viec. Value: TLL
    class DependencyTaskInfo
    {
        public string TaskName;
        public Dictionary<string, TypeLeadLag> DependingTasks = new Dictionary<string, TypeLeadLag>();

        public DependencyTaskInfo(string taskName)
        {
            TaskName = taskName;
        }
    }

    class TreeOfTasks
    {
        public Task RootTask; // La cong viec lon nhat hay chinh Project. Chua ten Project; La con tro chi toi moi nut con cua no (hay cong viec thuoc Project)
        public int latestLevelOneTaskID = 0;
        public bool automaticScheduling = true; // Dung de tu dong dieu chinh timeline Project sao cho hop ly nhat. 
        public CPMDiGraph graph = new CPMDiGraph(); // La cai do thi

        public Dictionary<string, string> TaskNameandIDDic = new Dictionary<string, string>(); //TaskName - ID. Tu dien luu tru cong viec va ID cua cong viec (A).
                                                                                               //Key: Ten cong viec A. Value: ID cong viec A
        public Dictionary<string, TaskIDandSubtasksID> TaskIDandSubTaskIDDic = new Dictionary<string, TaskIDandSubtasksID>(); //
        public Dictionary<string, DependencyTaskInfo> Dependencies = new Dictionary<string, DependencyTaskInfo>();  //

        public TreeOfTasks(string ProjectName)
        {
            RootTask = new Task("0", ProjectName);
            RootTask.IsLeafNode = false;

            TaskNameandIDDic.Add(ProjectName, "0");
            TaskIDandSubTaskIDDic.Add("0", new TaskIDandSubtasksID("0"));
        }

        public void AddTaskToRootTask(string TaskName)
        {
            if (AlreadyHaveThisTask(TaskName))
            {
                Console.WriteLine("There already exists a task with this name.");
                Console.WriteLine("Please change the task's name!");
            }
            else
            {
                Task newTask = new Task((++latestLevelOneTaskID).ToString(), TaskName);
                newTask.TaskNodeLevelInTree = 1;
                newTask.ParentTask = RootTask;
                RootTask.SubTasks.Add(latestLevelOneTaskID.ToString(), newTask);

                TaskNameandIDDic.Add(TaskName, latestLevelOneTaskID.ToString());
                TaskIDandSubTaskIDDic.Add(latestLevelOneTaskID.ToString(), new TaskIDandSubtasksID(latestLevelOneTaskID.ToString()));

                TaskIDandSubTaskIDDic["0"].latestPositionID++;
                TaskIDandSubTaskIDDic["0"].SubTaskIDS.Add(latestLevelOneTaskID.ToString());
            }
        }

        public void AddSubtaskToTask(string subTaskName, string TaskName)
        {
            if (AlreadyHaveThisTask(subTaskName))
            {
                Console.WriteLine("There already exists a task with this name.");
                Console.WriteLine("Please change the task's name!");
                return;
            }

            string TaskNameID = GetTaskID(TaskName);
            string subTaskNameID = MakeIDForSubtask(subTaskName, TaskName);

            if (subTaskNameID == "") return;

            TaskNameandIDDic.Add(subTaskName, subTaskNameID);
            TaskIDandSubTaskIDDic.Add(subTaskNameID, new TaskIDandSubtasksID(subTaskNameID));

            TaskIDandSubTaskIDDic[TaskNameID].latestPositionID++;
            TaskIDandSubTaskIDDic[TaskNameID].SubTaskIDS.Add(subTaskNameID);

            Task? Task = FindTaskNode(TaskName);

            if (Task == null) return;
            else
            {
                Task.IsLeafNode = false;

                Task subTask = new Task(subTaskNameID, subTaskName);
                subTask.ParentTask = Task;
                subTask.TaskNodeLevelInTree = Task.TaskNodeLevelInTree + 1;

                Task.SubTasks.Add(subTaskNameID, subTask);
            }
        }

        public void AddDependency(string TaskName, string DependingTaskName, string Type)
        {
            Task? Task = FindTaskNode(TaskName);
            Task? DependingTask = FindTaskNode(DependingTaskName);

            if (Task == null || DependingTask == null)
            {
                return;
            }
            else if (!(Type == "SS" || Type == "FF" || Type == "SF" || Type == "FS"))
            {
                Console.WriteLine("Please choose Type as follows: SS/FF/FS/SF");
                return;
            }
            else if (Dependencies.ContainsKey(TaskName) && Dependencies[TaskName].DependingTasks.ContainsKey(DependingTaskName) && Dependencies[TaskName].DependingTasks[DependingTaskName].Type == Type)
            {
                Console.WriteLine("This dependency already exists!");
                return;
            }

            bool AlreadyHasTask = graph.CheckIfVertexExists(TaskName);
            bool AlreadyHasDependingTask = graph.CheckIfVertexExists(DependingTaskName);
            graph.AddEdge(DependingTaskName, TaskName, Type);

            if (graph.CheckingLoop())
            {
                graph.RemoveEdge(DependingTaskName, TaskName);

                if (!AlreadyHasTask)
                {
                    graph.RemoveVertex(TaskName);
                }

                if (!AlreadyHasDependingTask)
                {
                    graph.RemoveVertex(DependingTaskName);
                }

                Console.WriteLine($"Dependency {DependingTaskName} -> {TaskName} creates loop!");
                return;
            }

            if (!Dependencies.ContainsKey(TaskName))
            {
                Dependencies.Add(TaskName, new DependencyTaskInfo(TaskName));
            }

            if (!Dependencies[TaskName].DependingTasks.ContainsKey(DependingTaskName))
            {
                Dependencies[TaskName].DependingTasks.Add(DependingTaskName, new TypeLeadLag(Type));
            }
            else if (Dependencies[TaskName].DependingTasks[DependingTaskName].Type != Type)
            {
                Dependencies[TaskName].DependingTasks[DependingTaskName].Type = Type;
            }

            //if new type
            if (AlreadySettingTimeline(TaskName) || AlreadySettingTimeline(DependingTaskName))
            {
                if (AlreadySettingTimeline(TaskName) && AlreadySettingTimeline(DependingTaskName))
                {
                    if (Type == "SS" && DateTime.Compare(Task.StartDate, DependingTask.StartDate) < 0)
                    {
                        SetTimeline(TaskName, DependingTask.StartDate, DependingTask.StartDate.AddDays(Task.Duration - 1));
                    }
                    else if (Type == "FF" && DateTime.Compare(Task.EndDate, DependingTask.EndDate) < 0)
                    {
                        SetTimeline(TaskName, DependingTask.EndDate.AddDays(-Task.Duration + 1), DependingTask.EndDate);
                    }
                    else if (Type == "FS" && DateTime.Compare(Task.StartDate, DependingTask.EndDate) <= 0)
                    {
                        SetTimeline(TaskName, DependingTask.EndDate.AddDays(1), DependingTask.EndDate.AddDays(1 + Task.Duration - 1));
                    }
                    else if (Type == "SF" && DateTime.Compare(Task.EndDate, DependingTask.StartDate) < 0)
                    {
                        SetTimeline(TaskName, DependingTask.StartDate.AddDays(-Task.Duration + 1), DependingTask.StartDate);
                    }
                }
                else if (!AlreadySettingTimeline(TaskName))
                {
                    if (Type == "SS")
                    {
                        SetTimeline(TaskName, DependingTask.StartDate, DependingTask.StartDate.AddDays(Task.Duration - 1));
                    }
                    else if (Type == "FF")
                    {
                        SetTimeline(TaskName, DependingTask.EndDate.AddDays(-Task.Duration + 1), DependingTask.EndDate);
                    }
                    else if (Type == "FS")
                    {
                        SetTimeline(TaskName, DependingTask.EndDate.AddDays(1), DependingTask.EndDate.AddDays(1 + Task.Duration - 1));
                    }
                    else if (Type == "SF")
                    {
                        SetTimeline(TaskName, DependingTask.StartDate.AddDays(-Task.Duration + 1), DependingTask.StartDate);
                    }
                }
                else if (!AlreadySettingTimeline(DependingTaskName))
                {
                    if (Type == "SS")
                    {
                        SetTimeline(DependingTaskName, Task.StartDate, Task.StartDate.AddDays(DependingTask.Duration - 1));
                    }
                    else if (Type == "FF")
                    {
                        SetTimeline(DependingTaskName, Task.EndDate.AddDays(-DependingTask.Duration + 1), Task.EndDate);
                    }
                    else if (Type == "FS")
                    {
                        SetTimeline(DependingTaskName, Task.StartDate.AddDays(-1 - DependingTask.Duration + 1), Task.StartDate.AddDays(-1));
                    }
                    else if (Type == "SF")
                    {
                        SetTimeline(DependingTaskName, Task.EndDate, Task.EndDate.AddDays(DependingTask.Duration - 1));
                    }
                }
            }
        }

        public void SetTimelineForTask(string TaskName, DateTime newStart, DateTime newEnd)
        {
            Task? Task = FindTaskNode(TaskName);

            if (Task == null) return;
            else if (Task.SubTasks.Count > 0 && automaticScheduling)
            {
                Console.WriteLine("Auto Scheduling mode is on. You cannot change the timeline of summary task.");
                return;
            }
            else SetTimeline(TaskName, newStart, newEnd);
        }

        public void SetTimeline(string TaskName, DateTime newStart, DateTime newEnd)
        {
            Task? Task = FindTaskNode(TaskName);
            TimeSpan span = newEnd - newStart;
            if (Task == null) return;

            if (Dependencies.ContainsKey(TaskName))
            {
                foreach (KeyValuePair<string, TypeLeadLag> depending in Dependencies[TaskName].DependingTasks)
                {
                    if (CreateConflict(TaskName, depending.Key, depending.Value.Type, newStart, newEnd) && AlreadySettingTimeline(depending.Key))
                    {
                        Console.WriteLine($"Conflict occurs with dependency {depending.Key} -> {TaskName}!");
                        return;
                    }
                }

                foreach (KeyValuePair<string, TypeLeadLag> depending in Dependencies[TaskName].DependingTasks)
                {
                    if (!AlreadySettingTimeline(depending.Key))
                    {
                        Task? dependingTask = FindTaskNode(depending.Key);

                        if (depending.Value.Type == "SS")
                        {
                            SetTimeline(depending.Key, newStart, newStart.AddDays(dependingTask.Duration - 1));
                        }
                        else if (depending.Value.Type == "FF")
                        {
                            SetTimeline(depending.Key, newEnd.AddDays(-dependingTask.Duration + 1), newEnd);
                        }
                        else if (depending.Value.Type == "FS")
                        {
                            SetTimeline(depending.Key, newStart.AddDays(-1 - dependingTask.Duration + 1), newStart.AddDays(-1));
                        }
                        else if (depending.Value.Type == "SF")
                        {
                            SetTimeline(depending.Key, newEnd, newEnd.AddDays(dependingTask.Duration - 1));
                        }
                    }
                }
            }

            Task.StartDate = newStart;
            Task.EndDate = newEnd;
            Task.Duration = span.Days + 1;

            if (graph.CheckIfVertexExists(TaskName))
            {
                foreach (KeyValuePair<string, string> depended in graph.vertices[TaskName].Depended_vertices)
                {
                    Task? dependedTask = FindTaskNode(depended.Key);

                    if (depended.Value == "SS")
                    {
                        if (!AlreadySettingTimeline(depended.Key))
                        {
                            SetTimeline(depended.Key, newStart, newStart.AddDays(dependedTask.Duration - 1));
                        }
                        else if (DateTime.Compare(dependedTask.StartDate, newStart) < 0)
                        {
                            SetTimeline(depended.Key, newStart, newStart.AddDays(dependedTask.Duration - 1));
                        }
                    }
                    else if (depended.Value == "FF")
                    {
                        if (!AlreadySettingTimeline(depended.Key))
                        {
                            SetTimeline(depended.Key, newEnd.AddDays(-dependedTask.Duration + 1), newEnd);
                        }
                        else if (DateTime.Compare(dependedTask.EndDate, newEnd) < 0)
                        {
                            SetTimeline(depended.Key, newEnd.AddDays(-dependedTask.Duration + 1), newEnd);
                        }
                    }
                    else if (depended.Value == "FS")
                    {
                        if (!AlreadySettingTimeline(depended.Key))
                        {
                            SetTimeline(depended.Key, newEnd.AddDays(1), newEnd.AddDays(1 + dependedTask.Duration - 1));
                        }
                        else if (DateTime.Compare(dependedTask.StartDate, newEnd) <= 0)
                        {
                            SetTimeline(depended.Key, newEnd.AddDays(1), newEnd.AddDays(1 + dependedTask.Duration - 1));
                        }
                    }
                    else if (depended.Value == "SF")
                    {
                        if (!AlreadySettingTimeline(depended.Key))
                        {
                            SetTimeline(depended.Key, newStart.AddDays(-dependedTask.Duration + 1), newStart);
                        }
                        else if (DateTime.Compare(dependedTask.EndDate, newStart) < 0)
                        {
                            SetTimeline(depended.Key, newStart.AddDays(-dependedTask.Duration + 1), newStart);
                        }
                    }
                }
            }

            if (Task.ParentTask != null)
            {
                List<DateTime> listOfSubtaskStartDates = new List<DateTime>();
                List<DateTime> listOfSubtaskEndDates = new List<DateTime>();

                foreach (KeyValuePair<string, Task> subTask in Task.ParentTask.SubTasks)
                {
                    if (AlreadySettingTimeline(subTask.Value.TaskName))
                    {
                        listOfSubtaskStartDates.Add(subTask.Value.StartDate);
                    }
                }

                foreach (KeyValuePair<string, Task> subTask in Task.ParentTask.SubTasks)
                {
                    if (AlreadySettingTimeline(subTask.Value.TaskName))
                    {
                        listOfSubtaskEndDates.Add(subTask.Value.EndDate);
                    }
                }

                DateTime EarliestDate = EarliestDateTime(listOfSubtaskStartDates);
                DateTime LatestDate = LatestDateTime(listOfSubtaskEndDates);
                SetTimeline(Task.ParentTask.TaskName, EarliestDate, LatestDate);
            }
        }

        public DateTime EarliestDateTime(List<DateTime> DateTimes)
        {
            DateTime min = DateTime.MaxValue;

            foreach (DateTime dateTime in DateTimes)
            {
                if (DateTime.Compare(min, dateTime) > 0)
                {
                    min = dateTime;
                }
            }

            return min;
        }

        public DateTime LatestDateTime(List<DateTime> DateTimes)
        {
            DateTime max = DateTime.MinValue;

            foreach (DateTime dateTime in DateTimes)
            {
                if (DateTime.Compare(max, dateTime) < 0)
                {
                    max = dateTime;
                }
            }

            return max;
        }

        public bool CreateConflict(string TaskName, string DependingTaskName, string Type, DateTime newStart, DateTime newEnd)
        {
            Task? DependingTask = FindTaskNode(DependingTaskName);

            if (Type == "SS" && DateTime.Compare(newStart, DependingTask.StartDate) < 0)
            {
                return true;
            }
            else if (Type == "FF" && DateTime.Compare(newEnd, DependingTask.EndDate) < 0)
            {
                return true;
            }
            else if (Type == "FS" && DateTime.Compare(newStart, DependingTask.EndDate) <= 0)
            {
                return true;
            }
            else if (Type == "SF" && DateTime.Compare(newEnd, DependingTask.StartDate) < 0)
            {
                return true;
            }
            else return false;
        }

        public void AddDescriptionToTask(string TaskName, string Description)
        {
            Task? task = FindTaskNode(TaskName);

            if (task == null) return;
            else task.Desription.Add(Description);
        }

        public void DeleteDescriptionOfTaskAtLine(string TaskName, int Line)
        {
            Task? task = FindTaskNode(TaskName);

            if (task == null) return;
            else task.Desription.RemoveAt(Line);
        }

        public void DeleteAllDescriptionOfTask(string TaskName)
        {
            Task? task = FindTaskNode(TaskName);

            if (task == null) return;
            else task.Desription.Clear();
        }

        public void SetPriorityOfTask(string TaskName, string Priority)
        {
            if (Priority == "" || Priority == "Not started" || Priority == "In progress" || Priority == "Completed" || Priority == "Cancelled")
            {
                Task? task = FindTaskNode(TaskName);

                if (task == null) return;
                else task.Priority = Priority;
            }
            else
            {
                Console.WriteLine("Please set priority in these categories: \"\"\\Not started\\In progress\\Completed\\Cancelled");
            }
        }

        public void SetTimeBudgetForTask(string TaskName, int hours)
        {
            if (hours <= 0)
            {
                Console.WriteLine("The value for time budget must be greater than zero!");
            }
            else
            {
                Task? task = FindTaskNode(TaskName);

                if (task == null) return;
                else task.TimeBudget = hours;
            }
        }

        public void AddMemberToTask(string TaskName, string MemberName)
        {
            Task? task = FindTaskNode(TaskName);

            if (task == null) return;
            else
            {
                while (task != null)
                {
                    task.AssignedTeamMembers.Add(MemberName);
                    task = task.ParentTask;
                }
            }
        }

        public Task? FindTaskNode(string TaskName)
        {
            if (!AlreadyHaveThisTask(TaskName))
            {
                Console.WriteLine($"\"{TaskName}\" does not exist!");
                return null;
            }
            else
            {
                string TaskNameID = GetTaskID(TaskName);

                int lengthOfID = 1;

                if (TaskNameID == "0")
                {
                    return RootTask;
                }

                Task x = RootTask.SubTasks[TaskNameID.Substring(0, lengthOfID)];

                while (x.TaskID != TaskNameID)
                {
                    x = x.SubTasks[TaskNameID.Substring(0, ++lengthOfID)];
                }

                return x;
            }
        }

        public string MakeIDForSubtask(string subTaskName, string TaskName)
        {
            if (!AlreadyHaveThisTask(TaskName))
            {
                Console.WriteLine($"\"{TaskName}\" does not exist!");
                return "";
            }
            else if (AlreadyHaveThisTask(subTaskName))
            {
                Console.WriteLine("There already exists a task with this name.");
                Console.WriteLine("Please change the task's name!");
                return "";
            }
            else
            {
                string TaskNameID = GetTaskID(TaskName);
                int suffixNumberID = TaskIDandSubTaskIDDic[TaskNameID].latestPositionID + 1;
                string subTaskID = TaskNameID + suffixNumberID;
                return subTaskID;
            }
        }

        public bool AlreadySettingTimeline(string TaskName)
        {
            Task? task = FindTaskNode(TaskName);

            if (task == null) return false;
            else if (task.StartDate == DateTime.MinValue || task.EndDate == DateTime.MinValue) return false;
            else return true;
        }

        public bool AlreadyHaveThisTask(string TaskName)
        {
            if (TaskNameandIDDic.ContainsKey(TaskName)) return true;
            else return false;
        }

        public string GetTaskID(string TaskName)
        {
            if (AlreadyHaveThisTask(TaskName)) return TaskNameandIDDic[TaskName];
            else return "";
        }

        public void Print(string text)
        {
            Console.Write(text);
        }
    }

    class Vertex
    {
        public string TasKName;
        public int Duration;
        public int Float;
        public Dictionary<string, string> Depended_vertices = new Dictionary<string, string>(); //Task - Type. Tu dien luu tru cong viec phu thuoc vao 1 cong viec (A) cu the khac.
                                                                                                //Key: Ten cong viec A; Value: Cac cong viec phu thuoc


        public Vertex(string name)
        {
            TasKName = name;
        }
    }

    // Summary
    // CPMDiGraph: Kiem tra vong lap (phu thuoc cong viec)

    class CPMDiGraph
    {
        public Dictionary<string, Vertex> vertices = new Dictionary<string, Vertex>(); //

        public CPMDiGraph() { }

        public void AddVertex(string TaskName)
        {
            if (!CheckIfVertexExists(TaskName))
            {
                vertices.Add(TaskName, new Vertex(TaskName));
            }
        }

        public void RemoveVertex(string TaskName)
        {
            if (CheckIfVertexExists(TaskName))
            {
                vertices.Remove(TaskName);

                foreach (KeyValuePair<string, Vertex> vertex in vertices)
                {
                    if (vertex.Value.Depended_vertices.ContainsKey(TaskName))
                    {
                        vertex.Value.Depended_vertices.Remove(TaskName);
                    }
                }
            }
        }

        public void AddEdge(string v1, string v2, string Type)
        {
            AddVertex(v1);
            AddVertex(v2);

            if (!CheckIfEdgeExists(v1, v2))
            {
                vertices[v1].Depended_vertices.Add(v2, Type);
            }
        }

        public void RemoveEdge(string v1, string v2)
        {
            if (CheckIfEdgeExists(v1, v2))
            {
                vertices[v1].Depended_vertices.Remove(v2);
            }
        }

        public bool CheckIfVertexExists(string vertex)
        {
            return vertices.ContainsKey(vertex);
        }

        public bool CheckIfEdgeExists(string v1, string v2)
        {
            return vertices[v1].Depended_vertices.ContainsKey(v2);
        }

        public bool CheckingLoop()
        {
            return CheckingLoop2(vertices.ElementAt(0).Key, new List<string>());
        }

        public bool CheckingLoop2(string expandVertex, List<string> visited)
        {
            if (visited.Contains(expandVertex))
            {
                return true;
            }
            else
            {
                visited.Add(expandVertex);

                foreach (KeyValuePair<string, string> neighbor in vertices[expandVertex].Depended_vertices)
                {
                    if (CheckingLoop2(neighbor.Key, visited))
                    {
                        return true;
                    }
                }

                visited.Remove(expandVertex);
                return false;
            }
        }
    }

    class Program
    {
        static void Main()
        {
            TreeOfTasks tree = new TreeOfTasks("Project Management");

            tree.AddTaskToRootTask("A");
            tree.AddTaskToRootTask("B");
            tree.AddTaskToRootTask("C");
            tree.AddTaskToRootTask("D");
            tree.AddTaskToRootTask("E");

            tree.SetTimelineForTask("A", new DateTime(2024, 10, 8), new DateTime(2024, 10, 10));
            tree.SetTimelineForTask("B", new DateTime(2024, 10, 11), new DateTime(2024, 10, 14));
            tree.SetTimelineForTask("C", new DateTime(2024, 10, 10), new DateTime(2024, 10, 14));
            tree.SetTimelineForTask("D", new DateTime(2024, 10, 14), new DateTime(2024, 10, 15));
            tree.SetTimelineForTask("E", new DateTime(2024, 10, 14), new DateTime(2024, 10, 14));

            tree.AddDependency("B", "A", "FS");
            tree.AddDependency("B", "C", "SS");
            tree.AddDependency("D", "B", "FS");
            tree.AddDependency("D", "E", "FF");

            tree.AddDependency("B", "C", "FS");

            Task? A = tree.FindTaskNode("A");
            Task? B = tree.FindTaskNode("B");
            Task? C = tree.FindTaskNode("C");
            Task? D = tree.FindTaskNode("D");
            Task? E = tree.FindTaskNode("E");

            Print($"{A.Duration}\n");
            Print($"{A.StartDate}\n");
            Print($"{A.EndDate}\n");
            Print($"{B.Duration}\n");
            Print($"{B.StartDate}\n");
            Print($"{B.EndDate}\n");
            Print($"{C.Duration}\n");
            Print($"{C.StartDate}\n");
            Print($"{C.EndDate}\n");
            Print($"{D.Duration}\n");
            Print($"{D.StartDate}\n");
            Print($"{D.EndDate}\n");
            Print($"{E.Duration}\n");
            Print($"{E.StartDate}\n");
            Print($"{E.EndDate}\n");

            Print($"\n\n\n{tree.RootTask.Duration}\n");
            Print($"{tree.RootTask.StartDate}\n");
            Print($"{tree.RootTask.EndDate}\n");
        }

        static void Print(string text)
        {
            Console.Write(text);
        }
    }
}