﻿using System;
using System.Collections.Generic;
using System.Transactions;

namespace C__Project_1
{
    class TaskIDandSubtasksID
    {
        public string TaskID;
        public int latestPositionID = 0;
        public List<string> SubTaskIDS = new List<string>();

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
        public string Priority = "";

        public Task(string taskID, string taskName)
        {
            TaskID = taskID;
            TaskName = taskName;
        }

        public int TaskNodeLevelInTree;
        public int TaskFloat;

        public DateTime StartDate = DateTime.MinValue;
        public DateTime EndDate = DateTime.MinValue;
        public int Duration = 1;
        public bool FirstTimeDuration = true;
        public int TimeBudget; //In hours

        public string Status = "";
        public int PercentageCompleted = 0; //For non-leaf node only?

        public bool IsLeafNode = true;
        public bool OnCriticalPath = false;

        public List<string> AssignedTeamMembers = new List<string>();

        public Task? ParentTask;
        public Dictionary<string, Task> SubTasks = new Dictionary<string, Task>();
    }

    class TypeLeadLag
    {
        public string Type;
        public int Lead = 0;
        public int Lag = 0;

        public TypeLeadLag(string type)
        {
            Type = type;
        }
    }

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
        public Task RootTask;
        public int latestLevelOneTaskID = 0;
        public bool automaticScheduling = true;
        public CPMDiGraph graph = new CPMDiGraph();

        public Dictionary<string, string> TaskNameandIDDic = new Dictionary<string, string>(); //TaskName - ID
        public Dictionary<string, TaskIDandSubtasksID> TaskIDandSubTaskIDDic = new Dictionary<string, TaskIDandSubtasksID>();
        public Dictionary<string, DependencyTaskInfo> Dependencies = new Dictionary<string, DependencyTaskInfo>();

        public TreeOfTasks(string ProjectName)
        {
            RootTask = new Task("0", ProjectName);
            RootTask.TaskID = "0";
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

                if (RootTask.FirstTimeDuration)
                {
                    RootTask.Duration = 0;
                    RootTask.FirstTimeDuration = false;
                }
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

                if (Task.FirstTimeDuration)
                {
                    Task.Duration = 0;
                    Task.FirstTimeDuration = false;
                }
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

        public void SetTimeline(string TaskName, DateTime newStart, DateTime newEnd)
        {
            Task? Task = FindTaskNode(TaskName);
            TimeSpan span = newEnd - newStart;

            if (Task == null) return;
            if(Task.ParentTask != null && AlreadySettingTimeline(Task.ParentTask.TaskName) && span.Days + 1 > Task.ParentTask.Duration)
            {
                Console.WriteLine($"The duration of {TaskName} ({span.Days + 1}) is longer than of {Task.ParentTask.TaskName} ({Task.ParentTask.Duration})");
                return;
            }

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
                        else if(DateTime.Compare(dependedTask.StartDate, newStart) < 0)
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
                        else if(DateTime.Compare(dependedTask.EndDate, newEnd) < 0)
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
                        else if(DateTime.Compare(dependedTask.StartDate, newEnd) <= 0)
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
                        else if(DateTime.Compare(dependedTask.EndDate, newStart) < 0)
                        {
                            SetTimeline(depended.Key, newStart.AddDays(-dependedTask.Duration + 1), newStart);
                        }
                    }
                }
            }

            if (Task.ParentTask != null && AlreadySettingTimeline(Task.ParentTask.TaskName))
            {               
                if(DateTime.Compare(Task.StartDate, Task.ParentTask.StartDate) < 0)
                {
                    SetTimeline(Task.ParentTask.TaskName, Task.StartDate, Task.StartDate.AddDays(Task.ParentTask.Duration - 1));
                }

                if(DateTime.Compare(Task.EndDate, Task.ParentTask.EndDate) > 0)
                {
                    SetTimeline(Task.ParentTask.TaskName, Task.EndDate.AddDays(-Task.ParentTask.Duration + 1), Task.EndDate);
                }
            }

            foreach (KeyValuePair<string, Task> subtask in Task.SubTasks)
            {
                if (AlreadySettingTimeline(subtask.Value.TaskName))
                {
                    Task? SubtaskNode = FindTaskNode(subtask.Value.TaskName);

                    if (DateTime.Compare(SubtaskNode.StartDate, Task.StartDate) < 0)
                    {
                        SetTimeline(subtask.Value.TaskName, Task.StartDate, Task.StartDate.AddDays(SubtaskNode.Duration - 1));
                    }

                    if (DateTime.Compare(SubtaskNode.EndDate, Task.EndDate) > 0)
                    {
                        SetTimeline(subtask.Value.TaskName, Task.EndDate.AddDays(-SubtaskNode.Duration + 1), Task.EndDate);
                    }
                }
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
        public Dictionary<string, string> Depended_vertices = new Dictionary<string, string>(); //Task - Type

        public Vertex(string name)
        {
            TasKName = name;
        }
    }

    class CPMDiGraph
    {
        public Dictionary<string, Vertex> vertices = new Dictionary<string, Vertex>();

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

            tree.SetTimeline("A", new DateTime(2024, 10, 8), new DateTime(2024, 10, 10));
            tree.SetTimeline("B", new DateTime(2024, 10, 11), new DateTime(2024, 10, 14));
            tree.SetTimeline("C", new DateTime(2024, 10, 10), new DateTime(2024, 10, 14));
            tree.SetTimeline("D", new DateTime(2024, 10, 14), new DateTime(2024, 10, 15));
            tree.SetTimeline("E", new DateTime(2024, 10, 14), new DateTime(2024, 10, 14));

            tree.AddDependency("B", "A", "FS");
            tree.AddDependency("B", "C", "SS");
            //tree.AddDependency("B", "A", "FS");
            tree.AddDependency("D", "B", "FS");
            tree.AddDependency("D", "E", "FF");

            tree.AddDependency("B", "C", "FS");

            //tree.SetTimeline("B", new DateTime(2024, 10, 11), new DateTime(2024, 10, 13));
            //tree.SetTimeline("D", new DateTime(2024, 10, 14), new DateTime(2024, 10, 15));

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
        }

        static void Print(string text)
        {
            Console.Write(text);
        }
    }
}