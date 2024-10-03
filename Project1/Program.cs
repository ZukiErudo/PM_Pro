using System;
using System.Collections.Generic;

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
        public DateTime DueDate = DateTime.MinValue;
        public int Duration;
        public int TimeBudget; //In hours

        public string Status = "";
        public int PercentageCompleted = 0; //For non-leaf node only?

        public bool IsLeafNode = true;
        public bool OnCriticalPath = false;

        public List<string> AssignedTeamMembers = new List<string>();

        public Task? ParentTask;
        public Dictionary<string, Task> SubTasks = new Dictionary<string, Task>();
    }

    class DependencyInfo
    {
        public string DependencyTaskName;
        public string DependencyType;
        public List<string> SubTasks = new List<string>();

        public DependencyInfo(string Name, string Type)
        {
            DependencyTaskName = Name;
            DependencyType = Type;
        }
    }

    class TaskDependenciesGraph
    {
        public string DependedTaskName;
        public Dictionary<string, DependencyInfo> TaskandItsSubTasks = new Dictionary<string, DependencyInfo>();

        public TaskDependenciesGraph(string Name)
        {
            DependedTaskName = Name;
        }

        public void AddTask(string TaskName, string DependencyType)
        {
            TaskandItsSubTasks.Add(TaskName, new DependencyInfo(TaskName, DependencyType));
        }

        public void AddSubTaskToTask(string SubtaskName, string TaskName)
        {
            TaskandItsSubTasks[TaskName].SubTasks.Add(SubtaskName);
            AddTask(SubtaskName, TaskandItsSubTasks[TaskName].DependencyType);
        }

        public void ChangeDependency(string TaskName, string NewType)
        {

        }

        public void DeleteDependency(string TaskName)
        {

        }
    }

    class TreeOfTasks
    {
        public Task RootTask;
        public int latestLevelOneTaskID = 0;
        public CPMDiGraph graph = new CPMDiGraph();

        public Dictionary<string, string> TaskNameandIDDic = new Dictionary<string, string>(); //TaskName - ID
        public Dictionary<string, TaskIDandSubtasksID> TaskIDandSubTaskIDDic = new Dictionary<string, TaskIDandSubtasksID>();
        public Dictionary<string, TaskDependenciesGraph> DependencyTasks = new Dictionary<string, TaskDependenciesGraph>(); //TaskName - Dependencies

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

                graph.AddVertex(TaskName);
            }
        }

        public void AddSubtaskToTask(string subTaskName, string TaskName)
        {
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
                if(Task.IsLeafNode)
                {
                    Task.IsLeafNode = false;
                }

                Task subTask = new Task(subTaskNameID, subTaskName);
                subTask.ParentTask = Task;
                subTask.TaskNodeLevelInTree = Task.TaskNodeLevelInTree + 1;

                Task.SubTasks.Add(subTaskNameID, subTask);

                if(graph.vertices.ContainsKey(TaskName))
                {
                    graph.RemoveVertex(TaskName);
                }

                graph.AddVertex(subTaskName);
            }
        }

        //Super hard ones are here

        public void AddDependency(string DependingTaskName, string TaskName, string Type)
        {
            if (!(Type == "SS" || Type == "FF" || Type == "SF" || Type == "FS"))
            {
                Console.WriteLine("Please choose dependency type: SS/FF/SF/FS");
                return;
            }

            Task? DependingTaskNode = FindTaskNode(DependingTaskName);
            Task? TaskNode = FindTaskNode(TaskName);

            if(DependingTaskNode == null || TaskNode == null) return;

            bool dependingTaskHasTimeline = DependingTaskNode.StartDate != DateTime.MinValue ? true : false;
            bool TaskHasTimeline = TaskNode.StartDate != DateTime.MinValue ? true : false;

            List<string> dependingLeafTasks = new List<string>();
            List<string> leafTasks = new List<string>();

            if(!DependingTaskNode.IsLeafNode)
            {
                if(dependingTaskHasTimeline)
                {
                    dependingLeafTasks = ListOfTasksEarliestOrLatestDates(DependingTaskName, Type[0].ToString(), true);
                }
                else
                {
                    dependingLeafTasks = LeafTasksOf(DependingTaskName);
                }
            }
            else
            {
                dependingLeafTasks.Add(DependingTaskName);
            }

            if (!TaskNode.IsLeafNode)
            {
                if (TaskHasTimeline)
                {
                    leafTasks = ListOfTasksEarliestOrLatestDates(TaskName, Type[1].ToString(), true);
                }
                else
                {
                    leafTasks = LeafTasksOf(TaskName);
                }
            }
            else
            {
                leafTasks.Add(TaskName);
            }

            if(dependingTaskHasTimeline && TaskHasTimeline)
            {
                if(CheckTimelineConstraintsbetweenTasks(dependingLeafTasks, leafTasks, Type))
                {
                    Console.WriteLine("Please change timeline!");
                    return;
                }
            }

            AddDependenciesBetweenLeafTasksIntoGraph(dependingLeafTasks, leafTasks);

            if(graph.CheckingLoop() == true)
            {
                Console.WriteLine("Loop found!");

                foreach(string dependingTask in dependingLeafTasks)
                {
                    foreach(string tsk in leafTasks)
                    {
                        if(graph.CheckIfEdgeExists(dependingTask, tsk))
                        {
                            graph.RemoveEdge(dependingTask, tsk);
                        }
                    }
                }

                return;
            }


        }

        public bool CheckTimelineConstraintsbetweenTasks(List<string>? DependingList, List<string>? List, string Type)
        {
            bool check = false;

            if (DependingList == null || List == null)
            {
                Console.WriteLine("Null Error!");
                return false;
            }

            if (Type == "SS")
            {
                foreach(string DependingTask in DependingList)
                {
                    Task? DependingNode = FindTaskNode(DependingTask);

                    foreach(string Task in List)
                    {
                        Task? TaskNode = FindTaskNode(Task);

                        if(DateTime.Compare(TaskNode.StartDate, DependingNode.StartDate) < 0)
                        {
                            check = true; 
                            break;
                        }
                    }

                    if(check == true)
                    {
                        break;
                    }
                }
            }
            else if(Type == "FF")
            {
                foreach (string DependingTask in DependingList)
                {
                    Task? DependingNode = FindTaskNode(DependingTask);

                    foreach (string Task in List)
                    {
                        Task? TaskNode = FindTaskNode(Task);

                        if (DateTime.Compare(TaskNode.DueDate, DependingNode.DueDate) < 0)
                        {
                            check = true;
                            break;
                        }
                    }

                    if (check == true)
                    {
                        break;
                    }
                }
            }
            else if(Type == "FS")
            {
                foreach (string DependingTask in DependingList)
                {
                    Task? DependingNode = FindTaskNode(DependingTask);

                    foreach (string Task in List)
                    {
                        Task? TaskNode = FindTaskNode(Task);

                        if (DateTime.Compare(TaskNode.StartDate, DependingNode.DueDate) < 0)
                        {
                            check = true;
                            break;
                        }
                    }

                    if (check == true)
                    {
                        break;
                    }
                }
            }
            else if(Type == "SF")
            {
                foreach (string DependingTask in DependingList)
                {
                    Task? DependingNode = FindTaskNode(DependingTask);

                    foreach (string Task in List)
                    {
                        Task? TaskNode = FindTaskNode(Task);

                        if (DateTime.Compare(TaskNode.DueDate, DependingNode.StartDate) < 0)
                        {
                            check = true;
                            break;
                        }
                    }

                    if (check == true)
                    {
                        break;
                    }
                }
            }

            return check;
        }

        public void AddDependenciesBetweenLeafTasksIntoGraph(List<string>? DependingList, List<string>? List) //not yy, experiment
        {
            if(DependingList == null || List == null)
            {
                Console.WriteLine("Null Error!");
                return;
            }

            foreach(string dependingTask in DependingList)
            {
                foreach(string Task in List)
                {
                    if(!graph.CheckIfEdgeExists(dependingTask, Task))
                    {
                        graph.AddEdge(dependingTask, Task);
                    }
                }
            }
        }

        public List<string>? ListOfTasksEarliestOrLatestDates(string TaskName, string Type, bool onlyLeafTasks) //with parents
        {
            List<string>? leafTasks = LeafTasksOf(TaskName);

            if(leafTasks == null) return null;

            Task? task = FindTaskNode(TaskName);
            Dictionary<string, int> TimespanOfeachLeafTask = new Dictionary<string, int>();
            
            if(Type == "S")
            {
                foreach (string value in leafTasks)
                {
                    Task? leafValue = FindTaskNode(value);
                    TimeSpan timespan = leafValue.StartDate - task.StartDate;
                    TimespanOfeachLeafTask.Add(value, timespan.Days);
                }
            }
            else if(Type == "F")
            {
                foreach (string value in leafTasks)
                {
                    Task? leafValue = FindTaskNode(value);
                    TimeSpan timespan = task.DueDate - leafValue.DueDate;
                    TimespanOfeachLeafTask.Add(value, timespan.Days);
                }
            }

            int smallestTimespan = 0;
            
            for(int i = 0; ; ++i)
            {
                if(TimespanOfeachLeafTask.ContainsValue(i))
                {
                    smallestTimespan = i;
                    break;
                }
            }
            
            Queue<string> tasksQueue = new Queue<string>();
            List<string> listOfTasks = new List<string>();

            foreach(KeyValuePair<string, int> var in TimespanOfeachLeafTask)
            {
                if(var.Value == smallestTimespan)
                {
                    tasksQueue.Enqueue(var.Key);
                }
            }

            List<string> processedTasks = new List<string>();

            while (tasksQueue.Count > 0 && !onlyLeafTasks)
            {
                string taskInQueue = tasksQueue.Dequeue();
                listOfTasks.Add(taskInQueue);

                Task? taskNode = FindTaskNode(taskInQueue);

                if(taskNode.ParentTask.TaskName != TaskName && !processedTasks.Contains(taskNode.ParentTask.TaskName))
                {
                    processedTasks.Add(taskNode.ParentTask.TaskName);
                }
            }

            return listOfTasks;
        }

        public List<string>? AllSubtasksOf(string TaskName)
        {
            Queue<string> tasksQueue = new Queue<string>();
            List<string> subtasks = new List<string>();

            tasksQueue.Enqueue(TaskName);

            while (tasksQueue.Count > 0)
            {
                string task = tasksQueue.Dequeue();

                Task? TaskNode = FindTaskNode(task);

                if (TaskNode == null) return null;

                subtasks.Add(task);

                foreach (KeyValuePair<string, Task> subtask in TaskNode.SubTasks)
                {
                    tasksQueue.Enqueue(subtask.Key);
                }
            }

            return subtasks;
        }

        public List<string>? LeafTasksOf(string TaskName)
        {
            Queue<string> tasksQueue = new Queue<string>();
            List<string> leafTasks = new List<string>();

            tasksQueue.Enqueue(TaskName);

            while (tasksQueue.Count > 0)
            {
                string task = tasksQueue.Dequeue();

                Task? TaskNode = FindTaskNode(task);

                if (TaskNode == null) return null;

                if(TaskNode.SubTasks.Count == 0)
                {
                    leafTasks.Add(task);
                }

                foreach(KeyValuePair<string, Task> subtask in TaskNode.SubTasks)
                {
                    tasksQueue.Enqueue(subtask.Key);
                }
            }

            return leafTasks;
        }

        //Trivial here
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
            else if (task.StartDate == DateTime.MinValue) return false;
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
    }

    class Vertex
    {
        public string TasKName;
        public int Duration;
        public int Float;
        public List<string> Depended_vertices = new List<string>();

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
            vertices.Add(TaskName, new Vertex(TaskName));
        }

        public void RemoveVertex(string TaskName) 
        {
            vertices.Remove(TaskName);

            foreach(KeyValuePair<string, Vertex> vertex in vertices)
            {
                if(vertex.Value.Depended_vertices.Contains(TaskName))
                {
                    vertex.Value.Depended_vertices.Remove(TaskName);
                }
            }
        }

        public void AddEdge(string v1, string v2)
        {
            vertices[v1].Depended_vertices.Add(v2);
        }

        public void RemoveEdge(string v1, string v2)
        {
            vertices[v1].Depended_vertices.Remove(v2);
        }

        public bool CheckIfEdgeExists(string v1, string v2)
        {
            return vertices[v1].Depended_vertices.Contains(v2);
        }

        public bool CheckingLoop()
        {
            bool check = false;

            Queue<string> vertexQueue = new Queue<string>();
            List<string> visitedVertex = new List<string>();

            foreach(KeyValuePair<string, Vertex> vertex in vertices)
            {
                vertexQueue.Enqueue(vertex.Key);
            }

            while(vertexQueue.Count > 0)
            {
                string vertex = vertexQueue.Dequeue();

                visitedVertex.Add(vertex);

                foreach(string neighbor in vertices[vertex].Depended_vertices)
                {
                    if(visitedVertex.Contains(neighbor))
                    {
                        check = true;   
                        break;
                    }
                }

                if(check == true)
                {
                    break;
                }
            }

            return check;
        }
    }

    class Program
    {
        static void Main()
        {

        }

        static void Print(string text)
        {
            Console.Write(text);
        }
    }
}
