using System;
using System.Collections.Generic;
using System.Net;
using System.Security;
using System.Threading.Tasks;
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
        public DateTime StartDate = DateTime.MinValue;
        public DateTime EndDate = DateTime.MinValue;
        public int Duration = 1;

        public string Status = "Not start";
        public int PercentageCompleted = 0;
        public bool IsLeafNode = true;

        public Task? ParentTask;
        public Dictionary<string, Task> SubTasks = new Dictionary<string, Task>();
    }

    class TypeLag
    {
        public string Type;
        public int Lag = 0;

        public TypeLag(string type)
        {
            Type = type;
        }
    }

    class DependencyTaskInfo
    {
        public string TaskName;
        public Dictionary<string, TypeLag> DependingTasks = new Dictionary<string, TypeLag>();

        public DependencyTaskInfo(string taskName)
        {
            TaskName = taskName;
        }
    }

    class TreeOfTasks
    {
        public Task RootTask;
        private int latestLevelOneTaskID = 0;
        public DateTime CurrentDate = new DateTime(2024, 10, 15);
        public PDMDiGraph graph = new PDMDiGraph();
        public Dictionary<string, string> TaskNameandIDDic = new Dictionary<string, string>();
        public Dictionary<string, TaskIDandSubtasksID> TaskIDandSubTaskIDDic = new Dictionary<string, TaskIDandSubtasksID>();
        public Dictionary<string, DependencyTaskInfo> Dependencies = new Dictionary<string, DependencyTaskInfo>();

        public TreeOfTasks(string ProjectName)
        {
            RootTask = new Task("0", ProjectName);
            RootTask.IsLeafNode = false;
            TaskNameandIDDic.Add(ProjectName, "0");
            TaskIDandSubTaskIDDic.Add("0", new TaskIDandSubtasksID("0"));
        }

        public void ChangeTaskName(string TaskName, string newTaskName)
        {
            if (AlreadyHaveThisTask(TaskName) && !AlreadyHaveThisTask(newTaskName))
            {
                Task? Task = FindTaskNode(TaskName);
                if (Task == null)
                {
                    Print($"Cannot change the name task {TaskName} does not exist!\n");
                    return;
                }

                Task.TaskName = newTaskName;
                TaskNameandIDDic.Add(newTaskName, TaskNameandIDDic[TaskName]);
                TaskNameandIDDic.Remove(TaskName);

                if (Dependencies.ContainsKey(TaskName))
                {
                    Dependencies.Add(newTaskName, Dependencies[TaskName]);
                    Dependencies[newTaskName].TaskName = newTaskName;
                    Dependencies.Remove(TaskName);
                }

                foreach (KeyValuePair<string, DependencyTaskInfo> dependencies in Dependencies)
                {
                    if (dependencies.Value.DependingTasks.ContainsKey(TaskName))
                    {
                        dependencies.Value.DependingTasks.Add(newTaskName, dependencies.Value.DependingTasks[TaskName]);
                        dependencies.Value.DependingTasks.Remove(TaskName);
                    }
                }

                if (graph.vertices.ContainsKey(TaskName)) graph.ChangeVertexName(TaskName, newTaskName);
            }
            else Print($"Cannot change name of task {TaskName} because {TaskName} does not exist or the new name {newTaskName} already exists!\n");
        }

        public void AddTaskToRootTask(string TaskName)
        {
            if (AlreadyHaveThisTask(TaskName))
            {
                Print($"There already exists a task with this name ({TaskName}).\n");
                Print("Please change the task's name!\n");
                return;
            }

            Task newTask = new Task((++latestLevelOneTaskID).ToString(), TaskName);
            newTask.TaskNodeLevelInTree = 1;
            newTask.ParentTask = RootTask;
            RootTask.SubTasks.Add(latestLevelOneTaskID.ToString(), newTask);

            TaskNameandIDDic.Add(TaskName, latestLevelOneTaskID.ToString());
            TaskIDandSubTaskIDDic.Add(latestLevelOneTaskID.ToString(), new TaskIDandSubtasksID(latestLevelOneTaskID.ToString()));

            TaskIDandSubTaskIDDic["0"].latestPositionID++;
            TaskIDandSubTaskIDDic["0"].SubTaskIDS.Add(latestLevelOneTaskID.ToString());
        }

        public void AddSubtaskToTask(string subTaskName, string TaskName)
        {
            if (AlreadyHaveThisTask(subTaskName))
            {
                Print($"There already exists a task with this name ({subTaskName}).\n");
                Print("Please change the task's name!\n");
                return;
            }

            string TaskNameID = GetTaskID(TaskName);
            string subTaskNameID = MakeIDForSubtask(subTaskName, TaskName);

            if (TaskNameID == "" || subTaskNameID == "")
            {
                Print($"Cannot add {subTaskName} to {TaskName}! Please check the name!\n");
                return;
            }

            TaskNameandIDDic.Add(subTaskName, subTaskNameID);
            TaskIDandSubTaskIDDic.Add(subTaskNameID, new TaskIDandSubtasksID(subTaskNameID));

            TaskIDandSubTaskIDDic[TaskNameID].latestPositionID++;
            TaskIDandSubTaskIDDic[TaskNameID].SubTaskIDS.Add(subTaskNameID);

            Task? Task = FindTaskNode(TaskName);

            if (Task == null) Print($"Cannot add {subTaskName} to {TaskName} because {TaskName} does not exist!\n");
            else
            {
                Task subTask = new Task(subTaskNameID, subTaskName);
                subTask.ParentTask = Task;
                subTask.TaskNodeLevelInTree = Task.TaskNodeLevelInTree + 1;

                Task.IsLeafNode = false;
                Task.SubTasks.Add(subTaskNameID, subTask);
            }
        }

        public void DeleteTask(string TaskName)
        {
            string TaskID = GetTaskID(TaskName);
            if (TaskID == "")
            {
                Print($"Cannot delete task {TaskName} because it does not exist!\n");
                return;
            }

            Stack<string> stack = new Stack<string>();
            Stack<string> postOrderTraversal = new Stack<string>();
            stack.Push(TaskID);

            while (stack.Count > 0)
            {
                string DeletedTaskID = stack.Pop();
                postOrderTraversal.Push(DeletedTaskID);

                foreach (string subtaskID in TaskIDandSubTaskIDDic[DeletedTaskID].SubTaskIDS)
                {
                    stack.Push(subtaskID);
                }
            }

            while (postOrderTraversal.Count > 0)
            {
                string DeletedTaskID = postOrderTraversal.Pop();
                string IDName = GetTaskNameFromID(DeletedTaskID);

                if (Dependencies.ContainsKey(IDName))
                {
                    foreach (KeyValuePair<string, TypeLag> depending in Dependencies[IDName].DependingTasks)
                    {
                        DeleteDependency(IDName, depending.Key);
                    }
                }

                if (graph.CheckIfVertexExists(IDName))
                {
                    foreach (KeyValuePair<string, TypeLag> depended in graph.vertices[IDName].Depended_vertices)
                    {
                        DeleteDependency(depended.Key, IDName);
                    }

                    graph.RemoveVertex(IDName);
                }

                Dependencies.Remove(IDName);
                TaskIDandSubTaskIDDic.Remove(DeletedTaskID);
                TaskNameandIDDic.Remove(IDName);
            }

            if (TaskID.Length == 1)
            {
                TaskIDandSubTaskIDDic["0"].SubTaskIDS.Remove(TaskID);
                RootTask.SubTasks.Remove(TaskID);
            }
            else
            {
                string ParentID = TaskID.Substring(0, TaskID.Length - 1);
                TaskIDandSubTaskIDDic[ParentID].SubTaskIDS.Remove(TaskID);

                Task? ParentTask = FindTaskNode(GetTaskNameFromID(ParentID));
                if (ParentTask == null) Print($"Error! ParentTask of {TaskName} cannot be found!\n");
                else ParentTask.SubTasks.Remove(TaskID);
            }
        }

        public List<string> FindLeafTasksOf(string TaskName)
        {
            string TaskID = GetTaskID(TaskName);
            if (TaskID == "")
            {
                Print($"Cannot find leaf tasks of {TaskName} because it does not exist!\n");
                return new List<string>();
            }

            Queue<string> queue = new Queue<string>();
            List<string> leafTaskIDs = new List<string>();
            List<string> leafTasks = new List<string>();

            queue.Enqueue(TaskID);

            while (queue.Count > 0)
            {
                string DeletedTaskID = queue.Dequeue();

                if (TaskIDandSubTaskIDDic[DeletedTaskID].SubTaskIDS.Count == 0) leafTaskIDs.Add(DeletedTaskID);

                foreach (string subtaskID in TaskIDandSubTaskIDDic[DeletedTaskID].SubTaskIDS)
                {
                    queue.Enqueue(subtaskID);
                }
            }

            foreach (string ID in leafTaskIDs)
            {
                leafTasks.Add(GetTaskNameFromID(ID));
            }

            return leafTasks;
        }

        public void AddDependency(string TaskName, string DependingTaskName, string Type)
        {
            Task? Task = FindTaskNode(TaskName);
            Task? DependingTask = FindTaskNode(DependingTaskName);
            if (Task == null || DependingTask == null)
            {
                Print($"Cannot make the dependency because {TaskName} or {DependingTaskName} does not exist!\n");
                return;
            }
            else if (!(Type == "SS" || Type == "FF" || Type == "SF" || Type == "FS"))
            {
                Print("Please choose Dependency Type as follows: SS/FF/FS/SF\n");
                return;
            }
            else if (Dependencies.ContainsKey(TaskName) && Dependencies[TaskName].DependingTasks.ContainsKey(DependingTaskName) && Dependencies[TaskName].DependingTasks[DependingTaskName].Type == Type)
            {
                Print("This dependency already exists!\n");
                return;
            }
            else if (GetTaskID(TaskName).Length != GetTaskID(DependingTaskName).Length)
            {
                string TaskID = GetTaskID(TaskName);
                string DependingTaskID = GetTaskID(DependingTaskName);

                string HigherLevelTaskID = DependingTaskID.Length < TaskID.Length ? DependingTaskID : TaskID;
                string LowerLevelTaskID = HigherLevelTaskID == DependingTaskID ? TaskID : DependingTaskID;

                if (HigherLevelTaskID == LowerLevelTaskID.Substring(0, HigherLevelTaskID.Length))
                {
                    Print($"Cannot make dependency {DependingTaskName} -> {TaskName} because they are in the same hierarchy!\n");
                    return;
                }
            }

            bool AlreadyHasTask = graph.CheckIfVertexExists(TaskName);
            bool AlreadyHasDependingTask = graph.CheckIfVertexExists(DependingTaskName);
            graph.AddEdge(DependingTaskName, TaskName, Type);

            if (graph.CheckingLoop())
            {
                graph.RemoveEdge(DependingTaskName, TaskName);

                if (!AlreadyHasTask) graph.RemoveVertex(TaskName);

                if (!AlreadyHasDependingTask) graph.RemoveVertex(DependingTaskName);

                Print($"Cannot add the dependency because this dependency {DependingTaskName} -> {TaskName} creates loop!\n");
                return;
            }

            if (!Dependencies.ContainsKey(TaskName))
            {
                Dependencies.Add(TaskName, new DependencyTaskInfo(TaskName));
            }

            if (!Dependencies[TaskName].DependingTasks.ContainsKey(DependingTaskName))
            {
                Dependencies[TaskName].DependingTasks.Add(DependingTaskName, new TypeLag(Type));
            }
            else if (Dependencies[TaskName].DependingTasks[DependingTaskName].Type != Type)
            {
                Dependencies[TaskName].DependingTasks[DependingTaskName].Type = Type;
            }

            graph.AddDurationTo(TaskName, Task.Duration);
            graph.AddDurationTo(DependingTaskName, DependingTask.Duration);

            if (AlreadySettingTimeline(TaskName) || AlreadySettingTimeline(DependingTaskName))
            {
                if (AlreadySettingTimeline(TaskName) && AlreadySettingTimeline(DependingTaskName))
                {
                    if (Type == "SS" && DateTime.Compare(Task.StartDate, DependingTask.StartDate) != 0)
                        SetTimeline(TaskName, DependingTask.StartDate, DependingTask.StartDate.AddDays(Task.Duration - 1));
                    else if (Type == "FF" && DateTime.Compare(Task.EndDate, DependingTask.EndDate) != 0)
                        SetTimeline(TaskName, DependingTask.EndDate.AddDays(-Task.Duration + 1), DependingTask.EndDate);
                    else if (Type == "FS")
                    {
                        TimeSpan span = Task.StartDate - DependingTask.EndDate;
                        if (span.Days != 1) SetTimeline(TaskName, DependingTask.EndDate.AddDays(1), DependingTask.EndDate.AddDays(1 + Task.Duration - 1));
                    }
                    else if (Type == "SF" && DateTime.Compare(Task.EndDate, DependingTask.StartDate) != 0)
                        SetTimeline(TaskName, DependingTask.StartDate.AddDays(-Task.Duration + 1), DependingTask.StartDate);
                }
                else if (!AlreadySettingTimeline(TaskName))
                {
                    if (Type == "SS") SetTimeline(TaskName, DependingTask.StartDate, DependingTask.StartDate.AddDays(Task.Duration - 1));
                    else if (Type == "FF") SetTimeline(TaskName, DependingTask.EndDate.AddDays(-Task.Duration + 1), DependingTask.EndDate);
                    else if (Type == "FS") SetTimeline(TaskName, DependingTask.EndDate.AddDays(1), DependingTask.EndDate.AddDays(1 + Task.Duration - 1));
                    else if (Type == "SF") SetTimeline(TaskName, DependingTask.StartDate.AddDays(-Task.Duration + 1), DependingTask.StartDate);
                }
                else if (!AlreadySettingTimeline(DependingTaskName))
                {
                    if (Type == "SS") SetTimeline(DependingTaskName, Task.StartDate, Task.StartDate.AddDays(DependingTask.Duration - 1));
                    else if (Type == "FF") SetTimeline(DependingTaskName, Task.EndDate.AddDays(-DependingTask.Duration + 1), Task.EndDate);
                    else if (Type == "FS") SetTimeline(DependingTaskName, Task.StartDate.AddDays(-1 - DependingTask.Duration + 1), Task.StartDate.AddDays(-1));
                    else if (Type == "SF") SetTimeline(DependingTaskName, Task.EndDate, Task.EndDate.AddDays(DependingTask.Duration - 1));
                }
            }
        }

        public void AddLagToDependency(string TaskName, string DependingTaskName, int Lag)
        {
            Task? Task = FindTaskNode(TaskName);
            Task? DependingTask = FindTaskNode(DependingTaskName);
            if (Task == null || DependingTask == null)
            {
                Print($"Cannot add lag because {TaskName} or {DependingTaskName} does not exist!\n");
                return;
            }
            else if (!Dependencies.ContainsKey(TaskName) || !Dependencies[TaskName].DependingTasks.ContainsKey(DependingTaskName))
            {
                Print($"Cannot add lag to dependency {DependingTaskName} -> {TaskName} because this dependency does not exist!\n");
                return;
            }
            else if (Dependencies[TaskName].DependingTasks[DependingTaskName].Lag == Lag)
            {
                Print($"The lag of dependency {DependingTaskName} -> {TaskName} already = {Lag}\n");
                return;
            }

            string Type = Dependencies[TaskName].DependingTasks[DependingTaskName].Type;
            Dependencies[TaskName].DependingTasks[DependingTaskName].Lag = Lag;
            graph.AddEdge(DependingTaskName, TaskName, Type, Lag);

            if (AlreadySettingTimeline(TaskName) && AlreadySettingTimeline(DependingTaskName))
            {
                TimeSpan span = new TimeSpan();

                if (Type == "SS")
                {
                    span = Task.StartDate - DependingTask.StartDate;
                    if (span.Days != Lag) SetTimeline(TaskName, DependingTask.StartDate.AddDays(Lag), DependingTask.StartDate.AddDays(Lag + Task.Duration - 1), DependingTaskName);
                }
                else if (Type == "FF")
                {
                    span = Task.EndDate - DependingTask.EndDate;
                    if (span.Days != Lag) SetTimeline(TaskName, DependingTask.EndDate.AddDays(Lag - Task.Duration + 1), DependingTask.EndDate.AddDays(Lag), DependingTaskName);
                }
                else if (Type == "FS")
                {
                    span = Task.StartDate - DependingTask.EndDate;
                    if (span.Days - 1 != Lag) SetTimeline(TaskName, DependingTask.EndDate.AddDays(1 + Lag), DependingTask.EndDate.AddDays(1 + Lag + Task.Duration - 1), DependingTaskName);
                }
                else if (Type == "SF")
                {
                    span = Task.EndDate - DependingTask.StartDate;
                    if (span.Days != Lag) SetTimeline(TaskName, DependingTask.StartDate.AddDays(Lag - Task.Duration + 1), DependingTask.StartDate.AddDays(Lag), DependingTaskName);
                }
            }
        }

        public void DeleteDependency(string TaskName, string DependingTaskName)
        {
            Task? Task = FindTaskNode(TaskName);
            Task? DependingTask = FindTaskNode(DependingTaskName);
            if (Task == null || DependingTask == null)
            {
                Print($"Cannot delete this dependency because {TaskName} or {DependingTaskName} does not exist!\n");
                return;
            }
            else if (!Dependencies.ContainsKey(TaskName) || !Dependencies[TaskName].DependingTasks.ContainsKey(DependingTaskName))
            {
                Print($"Cannot delete dependency {DependingTaskName} -> {TaskName} because it does not exist!\n");
                return;
            }

            Dependencies[TaskName].DependingTasks.Remove(DependingTaskName);
            graph.RemoveEdge(DependingTaskName, TaskName);

            if (AlreadySettingTimeline(TaskName) && AlreadySettingTimeline(DependingTaskName))
            {
                List<DateTime> listOfSubtaskStartDates = new List<DateTime>();

                foreach (KeyValuePair<string, TypeLag> depending in Dependencies[TaskName].DependingTasks)
                {
                    DependingTask = FindTaskNode(depending.Key);
                    if (DependingTask == null)
                    {
                        Print($"Error! Cannot find the depending task {DependingTaskName} of {TaskName}!\n");
                        return;
                    }

                    if (depending.Value.Type == "SS") listOfSubtaskStartDates.Add(DependingTask.StartDate.AddDays(depending.Value.Lag));
                    else if (depending.Value.Type == "FF") listOfSubtaskStartDates.Add(DependingTask.EndDate.AddDays(depending.Value.Lag - Task.Duration + 1));
                    else if (depending.Value.Type == "FS") listOfSubtaskStartDates.Add(DependingTask.EndDate.AddDays(1 + depending.Value.Lag));
                    else if (depending.Value.Type == "SF") listOfSubtaskStartDates.Add(DependingTask.StartDate.AddDays(depending.Value.Lag - Task.Duration + 1));
                }

                DateTime LatestStartDate = LatestDateTime(listOfSubtaskStartDates);
                SetTimeline(TaskName, LatestStartDate, LatestStartDate.AddDays(Task.Duration - 1));
            }
        }

        public void SetTimelineForTask(string TaskName, DateTime newStart, DateTime newEnd)
        {
            Task? Task = FindTaskNode(TaskName);
            if (Task == null) Print($"Cannot change the timeline of task {TaskName} because it does not exist!\n");
            else if (Task.SubTasks.Count > 0) Print($"Cannot change the timeline of summary task {TaskName}!\n");
            else if (newStart == DateTime.MinValue || newEnd == DateTime.MinValue) Print($"The timeline of a task must not be set to 1/1/0001!\n");
            else if (newStart > newEnd) Print($"Cannot set timeline because the finish date is before the start date!\n");
            else SetTimeline(TaskName, newStart, newEnd);
        }

        private void SetTimeline(string TaskName, DateTime newStart, DateTime newEnd, string excluded = "")
        {
            Task? Task = FindTaskNode(TaskName);
            TimeSpan span = newEnd - newStart;
            if (Task == null)
            {
                Print($"Cannot set timeline of task {TaskName} because it does not exist!\n");
                return;
            }

            if (Dependencies.ContainsKey(TaskName))
            {
                foreach (KeyValuePair<string, TypeLag> depending in Dependencies[TaskName].DependingTasks)
                {
                    if (depending.Key != excluded && CreateConflict(TaskName, depending.Key, depending.Value.Type, newStart, newEnd) && AlreadySettingTimeline(depending.Key))
                    {
                        Task? dependingTask = FindTaskNode(depending.Key);
                        if (dependingTask == null)
                        {
                            Print($"Error! Depending task {depending.Key} of {TaskName} does not exist!\n");
                            return;
                        }

                        Print($"Conflict occurs with dependency {depending.Key} -> {TaskName}!\n");
                        Print($"Type: {depending.Value.Type}\n");
                        Print($"Lag = {depending.Value.Lag}\n");
                        Print($"-Depending Task: {depending.Key}, ID = {dependingTask.TaskID}:\n");
                        Print($" +Start Date = {dependingTask.StartDate}\n");
                        Print($" +End Date = {dependingTask.EndDate}\n\n");
                        Print($"-Task: {TaskName}, ID = {Task.TaskID}:\n");
                        Print($" +New Start Date = {newStart}\n");
                        Print($" +New End Date = {newEnd}\n");
                        Print($" +Current Start Date = {Task.StartDate}\n");
                        Print($" +Current End Date = {Task.EndDate}\n\n");
                        return;
                    }
                }

                foreach (KeyValuePair<string, TypeLag> depending in Dependencies[TaskName].DependingTasks)
                {
                    if (!AlreadySettingTimeline(depending.Key))
                    {
                        Task? dependingTask = FindTaskNode(depending.Key);
                        if (dependingTask == null)
                        {
                            Print($"Cannot change timeline because depending task {depending.Key} does not exist!\n");
                            return;
                        }

                        int Lag = Dependencies[TaskName].DependingTasks[depending.Key].Lag;
                        string Type = depending.Value.Type;

                        if (Type == "SS") SetTimeline(depending.Key, newStart.AddDays(-Lag), newStart.AddDays(-Lag + dependingTask.Duration - 1));
                        else if (Type == "FF") SetTimeline(depending.Key, newEnd.AddDays(-Lag - dependingTask.Duration + 1), newEnd.AddDays(-Lag));
                        else if (Type == "FS") SetTimeline(depending.Key, newStart.AddDays(-1 - Lag - dependingTask.Duration + 1), newStart.AddDays(-1 - Lag));
                        else if (Type == "SF") SetTimeline(depending.Key, newEnd.AddDays(-Lag), newEnd.AddDays(-Lag + dependingTask.Duration - 1));
                    }
                }
            }

            Task.StartDate = newStart;
            Task.EndDate = newEnd;
            Task.Duration = span.Days + 1;

            if (graph.CheckIfVertexExists(TaskName))
            {
                graph.AddDurationTo(TaskName, Task.Duration);

                foreach (KeyValuePair<string, TypeLag> depended in graph.vertices[TaskName].Depended_vertices)
                {
                    Task? dependedTask = FindTaskNode(depended.Key);
                    if (dependedTask == null)
                    {
                        Print($"Cannot set timeline because the depended task {depended.Key} cannot be found!\n");
                        return;
                    }

                    int Lag = depended.Value.Lag;
                    string Type = depended.Value.Type;
                    TimeSpan newSpan = new TimeSpan();

                    if (Type == "SS")
                    {
                        newSpan = dependedTask.StartDate - Task.StartDate;
                        if (!AlreadySettingTimeline(depended.Key) || (newSpan.Days != Lag && Lag != 0))
                            SetTimeline(depended.Key, newStart.AddDays(Lag), newStart.AddDays(Lag + dependedTask.Duration - 1), TaskName);
                        else if (Lag == 0) SetTimeline(depended.Key, newStart, newStart.AddDays(dependedTask.Duration - 1));
                        // && DateTime.Compare(dependedTask.StartDate, newStart) < 0
                    }
                    else if (Type == "FF")
                    {
                        newSpan = dependedTask.EndDate - Task.EndDate;
                        if (!AlreadySettingTimeline(depended.Key) || (newSpan.Days != Lag && Lag != 0))
                            SetTimeline(depended.Key, newEnd.AddDays(Lag - dependedTask.Duration + 1), newEnd.AddDays(Lag), TaskName);
                        else if (Lag == 0) SetTimeline(depended.Key, newEnd.AddDays(-dependedTask.Duration + 1), newEnd);
                        // && DateTime.Compare(dependedTask.EndDate, newEnd) < 0
                    }
                    else if (Type == "FS")
                    {
                        newSpan = dependedTask.StartDate - Task.EndDate;
                        if (!AlreadySettingTimeline(depended.Key) || (newSpan.Days - 1 != Lag && Lag != 0))
                            SetTimeline(depended.Key, newEnd.AddDays(1 + Lag), newEnd.AddDays(1 + Lag + dependedTask.Duration - 1), TaskName);
                        else if (Lag == 0) SetTimeline(depended.Key, newEnd.AddDays(1), newEnd.AddDays(1 + dependedTask.Duration - 1));
                        // && DateTime.Compare(dependedTask.StartDate, newEnd) <= 0
                    }
                    else if (Type == "SF")
                    {
                        newSpan = dependedTask.EndDate - Task.StartDate;
                        if (!AlreadySettingTimeline(depended.Key) || (newSpan.Days != Lag && Lag != 0))
                            SetTimeline(depended.Key, newStart.AddDays(Lag - dependedTask.Duration + 1), newStart.AddDays(Lag), TaskName);
                        else if (Lag == 0) SetTimeline(depended.Key, newStart.AddDays(-dependedTask.Duration + 1), newStart);
                        // && DateTime.Compare(dependedTask.EndDate, newStart) < 0
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
                        listOfSubtaskEndDates.Add(subTask.Value.EndDate);
                    }
                }

                DateTime EarliestDate = EarliestDateTime(listOfSubtaskStartDates);
                DateTime LatestDate = LatestDateTime(listOfSubtaskEndDates);
                SetTimeline(Task.ParentTask.TaskName, EarliestDate, LatestDate);
            }
        }

        public void ChangeDuration(string TaskName, int newDuration)
        {
            Task? Task = FindTaskNode(TaskName);
            if (newDuration < 1)
            {
                Print($"Duration of a task must be greater than 1!\n");
                return;
            }
            else if (Task == null)
            {
                Print($"Cannot change duration of task {TaskName} because it does not exist!\n");
                return;
            }
            else if (!Task.IsLeafNode)
            {
                Print($"Cannot change duration because {TaskName} is a summary task!\n");
                return;
            }

            if (!AlreadySettingTimeline(TaskName))
            {
                Task.Duration = newDuration;
                return;
            }

            int difference = newDuration - Task.Duration;
            DateTime oldStartDate = Task.StartDate;
            SetTimeline(TaskName, Task.StartDate.AddDays(-difference), Task.EndDate);

            if (Task.StartDate != oldStartDate.AddDays(-difference))
            {
                DateTime oldEndDate = Task.EndDate;
                SetTimeline(TaskName, Task.StartDate, Task.EndDate.AddDays(difference));

                if (Task.EndDate != oldEndDate.AddDays(difference))
                {
                    Print($"Fail to change duration from {Task.Duration} to {newDuration} of task {TaskName}!\n");
                }
            }
        }

        public List<string> EarliestTasks(List<string> Tasks)
        {
            if (Tasks.Count == 0)
            {
                Print($"The list is empty!\n");
                Print($"Warning! Return empty list of strings.\n");
                return new List<string>();
            }

            List<string> earliestTasks = new List<string>();
            List<Task> TaskNodes = new List<Task>();

            foreach (string task in Tasks)
            {
                Task? TaskNode = FindTaskNode(task);
                if (TaskNode != null) TaskNodes.Add(TaskNode);
            }

            List<DateTime> listOfSubtaskStartDates = new List<DateTime>();

            foreach (Task TaskNode in TaskNodes)
            {
                if (AlreadySettingTimeline(TaskNode.TaskName))
                {
                    listOfSubtaskStartDates.Add(TaskNode.StartDate);
                }
            }

            DateTime EarliestDate = EarliestDateTime(listOfSubtaskStartDates);

            foreach (Task TaskNode in TaskNodes)
            {
                if (TaskNode.StartDate == EarliestDate)
                {
                    earliestTasks.Add(TaskNode.TaskName);
                }
            }

            if (earliestTasks.Count == 0) Print($"Warning! Return empty list of strings.\n");
            return earliestTasks;
        }

        public List<string> LatestTasks(List<string> Tasks)
        {
            if (Tasks.Count == 0)
            {
                Print($"The list is empty!\n");
                Print($"Warning! Return empty list of strings.\n");
                return new List<string>();
            }

            List<string> latestTasks = new List<string>();
            List<Task> TaskNodes = new List<Task>();

            foreach (string task in Tasks)
            {
                Task? TaskNode = FindTaskNode(task);
                if (TaskNode != null) TaskNodes.Add(TaskNode);
            }

            List<DateTime> listOfSubtaskEndDates = new List<DateTime>();

            foreach (Task TaskNode in TaskNodes)
            {
                if (AlreadySettingTimeline(TaskNode.TaskName))
                {
                    listOfSubtaskEndDates.Add(TaskNode.EndDate);
                }
            }

            DateTime LatestDate = LatestDateTime(listOfSubtaskEndDates);

            foreach (Task TaskNode in TaskNodes)
            {
                if (TaskNode.EndDate == LatestDate)
                {
                    latestTasks.Add(TaskNode.TaskName);
                }
            }

            if (latestTasks.Count == 0) Print($"Warning! Return empty list of strings.\n");
            return latestTasks;
        }

        private DateTime EarliestDateTime(List<DateTime> DateTimes)
        {
            DateTime min = DateTime.MaxValue;

            foreach (DateTime dateTime in DateTimes)
            {
                if (DateTime.Compare(min, dateTime) > 0)
                {
                    min = dateTime;
                }
            }

            if (DateTimes.Count == 0)
            {
                Print($"Warning! DateTimes list's length = 0!\n");
                Print($"Return {min}\n");
            }

            return min;
        }

        private DateTime LatestDateTime(List<DateTime> DateTimes)
        {
            DateTime max = DateTime.MinValue;

            foreach (DateTime dateTime in DateTimes)
            {
                if (DateTime.Compare(max, dateTime) < 0)
                {
                    max = dateTime;
                }
            }

            if (DateTimes.Count == 0)
            {
                Print($"Warning! DateTimes list's length = 0!\n");
                Print($"Return {max}\n");
            }

            return max;
        }

        private bool CreateConflict(string TaskName, string DependingTaskName, string Type, DateTime newStart, DateTime newEnd)
        {
            Task? DependingTask = FindTaskNode(DependingTaskName);
            if (DependingTask == null)
            {
                Print($"Depending task {DependingTaskName} of task {TaskName} cannot be found!\n");
                return false;
            }

            int Lag = Dependencies[TaskName].DependingTasks[DependingTaskName].Lag;
            TimeSpan newSpan = new TimeSpan();

            if (Lag == 0)
            {
                if (Type == "SS" && DateTime.Compare(newStart, DependingTask.StartDate) < 0) return true;
                else if (Type == "FF" && DateTime.Compare(newEnd, DependingTask.EndDate) < 0) return true;
                else if (Type == "FS" && DateTime.Compare(newStart, DependingTask.EndDate) <= 0) return true;
                else if (Type == "SF" && DateTime.Compare(newEnd, DependingTask.StartDate) < 0) return true;
                else return false;
            }
            else
            {
                if (Type == "SS")
                {
                    newSpan = newStart - DependingTask.StartDate;
                    if (newSpan.Days == Lag || DateTime.Compare(newStart, DependingTask.StartDate) >= 0) return false;
                    else return true;
                }
                else if (Type == "FF")
                {
                    newSpan = newEnd - DependingTask.EndDate;
                    if (newSpan.Days == Lag || DateTime.Compare(newEnd, DependingTask.EndDate) >= 0) return false;
                    else return true;
                }
                else if (Type == "FS")
                {
                    newSpan = newStart - DependingTask.EndDate;
                    if (newSpan.Days - 1 == Lag || DateTime.Compare(newStart, DependingTask.EndDate) > 0) return false;
                    else return true;
                }
                else if (Type == "SF")
                {
                    newSpan = newEnd - DependingTask.StartDate;
                    if (newSpan.Days == Lag || DateTime.Compare(newEnd, DependingTask.StartDate) >= 0) return false;
                    else return true;
                }
                else return false;
            }
        }

        public void UpdateStatus(string TaskName, string Status)
        {
            Task? Task = FindTaskNode(TaskName);
            if (!(Status == "Not start" || Status == "In progress" || Status == "Complete"))
            {
                Print("PLease choose status as follows: Not start/In progress/Complete\n");
                return;
            }
            else if (Task == null)
            {
                Print($"Cannot update the status of task {TaskName} because it does not exist!\n");
                return;
            }
            else if (!Task.IsLeafNode)
            {
                Print($"Cannot change the status of task {TaskName} because it is a summary task!\n");
                return;
            }

            if (Status == "In progress" && Dependencies.ContainsKey(TaskName))
            {
                foreach (KeyValuePair<string, TypeLag> depending in Dependencies[TaskName].DependingTasks)
                {
                    if (depending.Value.Type == "SS" || depending.Value.Type == "FS")
                    {
                        Task? DependingTask = FindTaskNode(depending.Key);
                        if (DependingTask == null)
                        {
                            Print($"Cannot found the depending task {depending.Key} of {TaskName} because it does not exist!\n");
                            return;
                        }

                        int Lag = depending.Value.Lag;
                        string Type = depending.Value.Type;

                        if (Lag == 0 && !(DependingTask.Status == "In progress" || DependingTask.Status == "Complete"))
                        {
                            Print($"Cannot set the status of {TaskName} to {Status} because of dependency {depending.Key} -> {TaskName} type {Type}{Lag}!\n");
                            Print($"Depending task {depending.Key} status: {DependingTask.Status}.\n");
                            return;
                        }
                        else if (Lag != 0)
                        {
                            if (Type == "SS" && DependingTask.Status == "Not start" && !(DateTime.Compare(CurrentDate, DependingTask.StartDate.AddDays(Lag)) >= 0 && DateTime.Compare(CurrentDate, Task.StartDate) >= 0))
                            {
                                Print($"Cannot set the status of {TaskName} to {Status} because of dependency {depending.Key} -> {TaskName} type {Type}{Lag}!\n");
                                Print($"Depending task {depending.Key} status: {DependingTask.Status}.\n");
                                Print($"Current Date: {CurrentDate}\n");
                                Print($"DependingTask {depending.Key} StartDate: {DependingTask.StartDate}\n");
                                Print($"DependingTask {depending.Key} StartDate with Lag {Lag}: {DependingTask.StartDate.AddDays(Lag)}\n");
                                Print($"Task {TaskName} StartDate: {Task.StartDate}\n");
                                return;
                            }
                            else if (Type == "FS" && DependingTask.Status != "Complete" && !(DateTime.Compare(CurrentDate, DependingTask.EndDate.AddDays(1 + Lag)) >= 0 && DateTime.Compare(CurrentDate, Task.StartDate) >= 0))
                            {
                                Print($"Cannot set the status of {TaskName} to {Status} because of dependency {depending.Key} -> {TaskName} type {Type}{Lag}!\n");
                                Print($"Depending task {depending.Key} status: {DependingTask.Status}.\n");
                                Print($"Current Date: {CurrentDate}\n");
                                Print($"DependingTask {depending.Key} EndDate: {DependingTask.EndDate}\n");
                                Print($"DependingTask {depending.Key} EndDate with Lag {Lag}: {DependingTask.EndDate.AddDays(1 + Lag)}\n");
                                Print($"Task {TaskName} StartDate: {Task.StartDate}\n");
                                return;
                            }
                        }
                    }
                }
            }
            else if (Status == "Complete" && Dependencies.ContainsKey(TaskName))
            {
                foreach (KeyValuePair<string, TypeLag> depending in Dependencies[TaskName].DependingTasks)
                {
                    if (depending.Value.Type == "FF" || depending.Value.Type == "SF")
                    {
                        Task? DependingTask = FindTaskNode(depending.Key);
                        if (DependingTask == null)
                        {
                            Print($"Cannot found the depending task {depending.Key} of {TaskName} because it does not exist!\n");
                            return;
                        }

                        int Lag = depending.Value.Lag;
                        string Type = depending.Value.Type;

                        if (Lag == 0 && !(DependingTask.Status == "In progress" || DependingTask.Status == "Complete"))
                        {
                            Print($"Cannot set the status of {TaskName} to {Status} because of dependency {depending.Key} -> {TaskName} type {Type}{Lag}!\n");
                            Print($"Depending task {depending.Key} status: {DependingTask.Status}.\n");
                            return;
                        }
                        else if (Lag != 0)
                        {
                            if (Type == "FF" && DependingTask.Status != "Complete" && !(DateTime.Compare(CurrentDate, DependingTask.EndDate.AddDays(Lag)) >= 0 && DateTime.Compare(CurrentDate, Task.EndDate) >= 0))
                            {
                                Print($"Cannot set the status of {TaskName} to {Status} because of dependency {depending.Key} -> {TaskName} type {Type}{Lag}!\n");
                                Print($"Depending task {depending.Key} status: {DependingTask.Status}.\n");
                                Print($"Current Date: {CurrentDate}\n");
                                Print($"DependingTask {depending.Key} EndDate: {DependingTask.EndDate}\n");
                                Print($"DependingTask {depending.Key} EndDate with Lag {Lag}: {DependingTask.EndDate.AddDays(Lag)}\n");
                                Print($"Task {TaskName} EndDate: {Task.EndDate}\n");
                                return;
                            }
                            else if (Type == "SF" && DependingTask.Status == "Not start" && !(DateTime.Compare(CurrentDate, DependingTask.StartDate.AddDays(Lag)) >= 0 && DateTime.Compare(CurrentDate, Task.EndDate) >= 0))
                            {
                                Print($"Cannot set the status of {TaskName} to {Status} because of dependency {depending.Key} -> {TaskName} type {Type}{Lag}!\n");
                                Print($"Depending task {depending.Key} status: {DependingTask.Status}.\n");
                                Print($"Current Date: {CurrentDate}\n");
                                Print($"DependingTask {depending.Key} StartDate: {DependingTask.StartDate}\n");
                                Print($"DependingTask {depending.Key} StartDate with Lag {Lag}: {DependingTask.StartDate.AddDays(Lag)}\n");
                                Print($"Task {TaskName} EndDate: {Task.EndDate}\n");
                                return;
                            }
                        }
                    }
                }
            }

            Task.Status = Status;
            UpdateStatusToParentTaskOfTask(TaskName);
            if (Status == "Complete") UpdatePercentageComplete(TaskName, 100);
        }

        private void UpdateStatusToParentTaskOfTask(string TaskName)
        {
            Task? Task = FindTaskNode(TaskName);
            if (Task == null || Task.ParentTask == null) return;

            if (Task.Status == "Not start" && Task.ParentTask.Status != "Not start")
            {
                bool statusChangeable = true;

                foreach (KeyValuePair<string, Task> subtask in Task.ParentTask.SubTasks)
                {
                    if (subtask.Value.Status != "Not start")
                    {
                        statusChangeable = false;
                        break;
                    }
                }

                if (statusChangeable)
                {
                    Task.ParentTask.Status = "Not start";
                    UpdateStatusToParentTaskOfTask(Task.ParentTask.TaskName);
                }
            }
            else if (Task.Status == "In progress" && (Task.ParentTask.Status == "Not start" || Task.ParentTask.Status == "Complete"))
            {
                Task.ParentTask.Status = "In progress";
                UpdateStatusToParentTaskOfTask(Task.ParentTask.TaskName);
            }
            else if (Task.Status == "Complete" && Task.ParentTask.Status != "Complete")
            {
                bool statusChangeable = true;

                foreach (KeyValuePair<string, Task> subtask in Task.ParentTask.SubTasks)
                {
                    if (subtask.Value.Status != "Complete")
                    {
                        statusChangeable = false;
                        break;
                    }
                }

                if (statusChangeable)
                {
                    Task.ParentTask.Status = "Complete";
                    UpdateStatusToParentTaskOfTask(Task.ParentTask.TaskName);
                }
                else if (Task.ParentTask.Status == "Not start")
                {
                    Task.ParentTask.Status = "In progress";
                    UpdateStatusToParentTaskOfTask(Task.ParentTask.TaskName);
                }
            }
        }

        private void UpdatePercentageComplete(string TaskName, int Percentage)
        {
            Task? Task = FindTaskNode(TaskName);
            if (Task == null)
            {
                Print($"Cannot update task {TaskName} because it does not exist!\n");
                return;
            }

            Task.PercentageCompleted = Percentage;
            if (Task.ParentTask == null) return;
            int sum = 0;

            if (Task.ParentTask.Status == "Complete") UpdatePercentageComplete(Task.ParentTask.TaskName, 100);
            else
            {
                foreach (KeyValuePair<string, Task> subtask in Task.ParentTask.SubTasks)
                {
                    sum += subtask.Value.PercentageCompleted;
                }

                if (Task.ParentTask.SubTasks.Count == 0) Print($"Error! Parent Task {Task.ParentTask.TaskName} of {TaskName} has no Subtasks!\n");
                else UpdatePercentageComplete(Task.ParentTask.TaskName, sum / Task.ParentTask.SubTasks.Count);
            }
        }

        public void AddDescriptionToTask(string TaskName, string Description)
        {
            Task? task = FindTaskNode(TaskName);
            if (task == null) Print($"Cannot add description to task {TaskName} because it does not exist!\n");
            else task.Desription.Add(Description);
        }

        public void DeleteDescriptionOfTaskAtLine(string TaskName, int Line)
        {
            Task? task = FindTaskNode(TaskName);
            if (task == null) Print($"Cannot delete description from task {TaskName} because it does not exist!\n");
            else
            {
                if (Line > task.Desription.Count || Line <= 0) Print($"Cannot delete description at line {Line}!\n");
                else task.Desription.RemoveAt(Line - 1);
            }
        }

        public void DeleteAllDescriptionOfTask(string TaskName)
        {
            Task? task = FindTaskNode(TaskName);
            if (task == null) Print($"Cannot delete all description from task {TaskName} because it does not exist!\n");
            else task.Desription.Clear();
        }

        public void SetPriorityOfTask(string TaskName, string Priority)
        {
            if (Priority == "" || Priority == "Low" || Priority == "Medium" || Priority == "High")
            {
                Task? task = FindTaskNode(TaskName);
                if (task == null) Print($"Cannot set priority to task {TaskName} because it does not exist!\n");
                else task.Priority = Priority;
            }
            else Print("Please set priority in these categories: Low/Medium/High or put empty\n");
        }

        public Task? FindTaskNode(string TaskName)
        {
            if (!AlreadyHaveThisTask(TaskName))
            {
                Print($"Cannot find \"{TaskName}\" because it does not exist!\n");
                return null;
            }
            else
            {
                string TaskNameID = GetTaskID(TaskName);
                int lengthOfID = 1;
                if (TaskNameID == "0") return RootTask;

                Task x = RootTask.SubTasks[TaskNameID.Substring(0, lengthOfID)];

                while (x.TaskID != TaskNameID)
                {
                    x = x.SubTasks[TaskNameID.Substring(0, ++lengthOfID)];
                }

                return x;
            }
        }

        private string MakeIDForSubtask(string subTaskName, string TaskName)
        {
            if (!AlreadyHaveThisTask(TaskName))
            {
                Print($"Cannot make ID for task \"{TaskName}\" because it does not exist!\n");
                return "";
            }
            else if (AlreadyHaveThisTask(subTaskName))
            {
                Print("There already exists a task with this name.\n");
                Print("Please change the task's name!\n");
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
            if (task == null)
            {
                Print($"Cannot check the timeline of {TaskName} because it does not exist!\n");
                return false;
            }
            else if (task.StartDate == DateTime.MinValue || task.EndDate == DateTime.MinValue) return false;
            else return true;
        }

        public bool AlreadyHaveThisTask(string TaskName)
        {
            if (TaskNameandIDDic.ContainsKey(TaskName)) return true;
            else return false;
        }

        public string GetTaskNameFromID(string TaskID)
        {
            string TaskName = "";

            foreach (KeyValuePair<string, string> name in TaskNameandIDDic)
            {
                if (name.Value == TaskID)
                {
                    TaskName = name.Key;
                    break;
                }
            }

            if (TaskName == "")
            {
                Print($"Cannot find the task's name of ID {TaskID} because this ID does not exist!\n");
            }

            return TaskName;
        }

        public string GetTaskID(string TaskName)
        {
            if (AlreadyHaveThisTask(TaskName)) return TaskNameandIDDic[TaskName];
            else
            {
                Print($"Cannot find ID from task {TaskName} because {TaskName} does not exist!\n");
                return "";
            }
        }

        public void PrintAllTasksInfo(string TaskID = "0")
        {
            PrintFullInfoFromID(TaskID);
            Print("\n");

            foreach (string SubtaskID in TaskIDandSubTaskIDDic[TaskID].SubTaskIDS)
            {
                PrintAllTasksInfo(SubtaskID);
            }
        }

        public void PrintInfoOfTask(string TaskName)
        {
            PrintFullInfoFromID(GetTaskID(TaskName));
        }

        private void PrintFullInfoFromID(string TaskID)
        {
            Task? Task = FindTaskNode(GetTaskNameFromID(TaskID));
            if (Task == null)
            {
                Print($"Cannot find information from task with ID {TaskID}\n");
                return;
            }

            Print($"Task ID: {Task.TaskID}\n");
            Print($"Task Name: {Task.TaskName}\n");
            Print($"Duration: {Task.Duration}\n");
            Print($"Start Date: {Task.StartDate}\n");
            Print($"Finish Date: {Task.EndDate}\n");
            Print($"Status: {Task.Status}\n");
            Print($"Percenteage complete: {Task.PercentageCompleted}\n");
            Print($"Priority: {Task.Priority}\n");
            Print($"Time budget: {Task.TimeBudget}\n");
            Print($"Assigned members: ");
            foreach (string member in Task.AssignedTeamMembers)
            {
                Print($"{member} ");
            }
            Print("\n");
        }

        private void Print(string text)
        {
            Console.Write(text);
        }
    }

    class Vertex
    {
        public string TaskName;
        public int EarliestStart;
        public int EarliestEnd;
        public int LatestStart;
        public int LatestEnd;
        public int Duration;
        public int TotalFloat;

        public DateTime ES;
        public DateTime EF;
        public DateTime LS;
        public DateTime LF;

        public Dictionary<string, TypeLag> Depending_vertices = new Dictionary<string, TypeLag>();//TaskName - TypeLag
        public Dictionary<string, TypeLag> Depended_vertices = new Dictionary<string, TypeLag>(); //TaskName - TypeLag

        public Vertex(string name)
        {
            TaskName = name;
        }
    }

    class PDMDiGraph
    {
        public Dictionary<string, Vertex> vertices = new Dictionary<string, Vertex>();
        public Vertex Start = new Vertex("Start");
        public Vertex End = new Vertex("End");

        public PDMDiGraph()
        {
            Start.EarliestStart = -1;
            Start.EarliestEnd = -1;
        }

        public void ChangeVertexName(string vertex, string newVertex)
        {
            if (vertices.ContainsKey(vertex) && !vertices.ContainsKey(newVertex))
            {
                foreach (KeyValuePair<string, Vertex> v in vertices)
                {
                    if (v.Key != vertex)
                    {
                        if (v.Value.Depending_vertices.ContainsKey(vertex))
                        {
                            v.Value.Depending_vertices.Add(newVertex, v.Value.Depending_vertices[vertex]);
                            v.Value.Depending_vertices.Remove(vertex);
                        }

                        if (v.Value.Depended_vertices.ContainsKey(vertex))
                        {
                            v.Value.Depended_vertices.Add(newVertex, v.Value.Depended_vertices[vertex]);
                            v.Value.Depended_vertices.Remove(vertex);
                        }
                    }
                }

                vertices.Add(newVertex, vertices[vertex]);
                vertices[newVertex].TaskName = newVertex;
                vertices.Remove(vertex);
            }
            else Print($"Cannot change {vertex} to {newVertex} because vertex {vertex} does not exist or the vertex's new name {newVertex} already exist!\n");
        }

        public void CalculateStartAndEnd()
        {
            Start = new Vertex("Start");
            End = new Vertex("End");

            Start.EarliestStart = -1;
            Start.EarliestEnd = -1;

            List<Vertex> verticesWithoutPredecessor = new List<Vertex>();

            foreach (KeyValuePair<string, Vertex> vertex in vertices)
            {
                if (vertex.Value.Depending_vertices.Count == 0)
                {
                    if (vertex.Value.ES == DateTime.MinValue || vertex.Value.EF == DateTime.MinValue)
                    {
                        Print($"Timeline of task {vertex.Key} has not been set!\n");
                        Print($"Cannot begin the process of calculating Start Date and Finish Date of tasks.\n");
                        return;
                    }
                    else verticesWithoutPredecessor.Add(vertex.Value);
                }
            }

            DateTime minES = DateTime.MaxValue;

            foreach (Vertex v in verticesWithoutPredecessor)
            {
                if (v.ES < minES)
                {
                    minES = v.ES;
                }
            }

            Start.ES = minES.AddDays(-1);
            Start.EF = minES.AddDays(-1);

            vertices.Add("Start", Start);
            vertices.Add("End", End);

            foreach (KeyValuePair<string, Vertex> vertex in vertices)
            {
                if (vertex.Value.Depending_vertices.Count == 0 && vertex.Key != "Start")
                {
                    TimeSpan Lag = vertex.Value.ES - Start.EF;
                    AddEdge("Start", vertex.Key, "FS", Lag.Days - 1);
                }

                if (vertex.Value.Depended_vertices.Count == 0 && vertex.Key != "End") AddEdge(vertex.Key, "End", "FS");
            }

            Queue<string> TopoSorted = SortTopology();
            Stack<string> TopoSorted2 = new Stack<string>();

            foreach (string vertex in TopoSorted)
            {
                TopoSorted2.Push(vertex);
            }

            ForwardPass(TopoSorted);

            vertices["End"].LatestStart = vertices["End"].EarliestStart;
            vertices["End"].LatestEnd = vertices["End"].EarliestEnd;

            vertices["End"].LS = vertices["End"].ES;
            vertices["End"].LF = vertices["End"].EF;

            End.EarliestStart = vertices["End"].EarliestStart;
            End.EarliestEnd = vertices["End"].EarliestEnd;
            End.LatestStart = vertices["End"].LatestStart;
            End.LatestEnd = vertices["End"].LatestEnd;

            End.ES = vertices["End"].ES;
            End.EF = vertices["End"].EF;
            End.LS = vertices["End"].LS;
            End.LF = vertices["End"].LF;

            BackwardPass(TopoSorted2);

            --End.EarliestStart;
            --End.EarliestEnd;
            --End.LatestStart;
            --End.LatestEnd;

            End.ES = End.ES.AddDays(-1);
            End.EF = End.EF.AddDays(-1);
            End.LS = End.LS.AddDays(-1);
            End.LF = End.LF.AddDays(-1);

            RemoveVertex("Start");
            RemoveVertex("End");
        }

        private void ForwardPass(Queue<string> TopologySorted)
        {
            TopologySorted.Dequeue();

            while (TopologySorted.Count > 0)
            {
                string TaskName = TopologySorted.Dequeue();
                int MaxEarlyStart = int.MinValue;

                foreach (KeyValuePair<string, TypeLag> dependeingTask in vertices[TaskName].Depending_vertices)
                {
                    if (dependeingTask.Value.Type == "SS" && vertices[dependeingTask.Key].EarliestStart + dependeingTask.Value.Lag > MaxEarlyStart)
                    {
                        MaxEarlyStart = vertices[dependeingTask.Key].EarliestStart + dependeingTask.Value.Lag;
                        vertices[TaskName].ES = vertices[dependeingTask.Key].ES.AddDays(dependeingTask.Value.Lag);
                    }
                    else if (dependeingTask.Value.Type == "FF" && vertices[dependeingTask.Key].EarliestEnd + dependeingTask.Value.Lag - vertices[TaskName].Duration + 1 > MaxEarlyStart)
                    {
                        MaxEarlyStart = vertices[dependeingTask.Key].EarliestEnd + dependeingTask.Value.Lag - vertices[TaskName].Duration + 1;
                        vertices[TaskName].ES = vertices[dependeingTask.Key].EF.AddDays(dependeingTask.Value.Lag - vertices[TaskName].Duration + 1);
                    }
                    else if (dependeingTask.Value.Type == "FS" && vertices[dependeingTask.Key].EarliestEnd + 1 + dependeingTask.Value.Lag > MaxEarlyStart)
                    {
                        MaxEarlyStart = vertices[dependeingTask.Key].EarliestEnd + dependeingTask.Value.Lag + 1;
                        vertices[TaskName].ES = vertices[dependeingTask.Key].EF.AddDays(dependeingTask.Value.Lag + 1);
                    }
                    else if (dependeingTask.Value.Type == "SF" && vertices[dependeingTask.Key].EarliestStart + dependeingTask.Value.Lag - vertices[TaskName].Duration + 1 > MaxEarlyStart)
                    {
                        MaxEarlyStart = vertices[dependeingTask.Key].EarliestStart + dependeingTask.Value.Lag - vertices[TaskName].Duration + 1;
                        vertices[TaskName].ES = vertices[dependeingTask.Key].ES.AddDays(dependeingTask.Value.Lag - vertices[TaskName].Duration + 1);
                    }
                }

                vertices[TaskName].EarliestStart = MaxEarlyStart;

                if (TaskName == "End")
                {
                    vertices[TaskName].EarliestEnd = MaxEarlyStart + vertices[TaskName].Duration;
                    vertices[TaskName].EF = vertices[TaskName].ES.AddDays(vertices[TaskName].Duration);
                }
                else
                {
                    vertices[TaskName].EarliestEnd = MaxEarlyStart + vertices[TaskName].Duration - 1;
                    vertices[TaskName].EF = vertices[TaskName].ES.AddDays(vertices[TaskName].Duration - 1);
                }
            }
        }

        private void BackwardPass(Stack<string> TopologySorted)
        {
            TopologySorted.Pop();

            while (TopologySorted.Count > 0)
            {
                string TaskName = TopologySorted.Pop();
                int MinLatestEnd = int.MaxValue;

                foreach (KeyValuePair<string, TypeLag> dependedTask in vertices[TaskName].Depended_vertices)
                {
                    if (dependedTask.Value.Type == "SS" && vertices[dependedTask.Key].LatestStart - dependedTask.Value.Lag + vertices[TaskName].Duration - 1 < MinLatestEnd)
                    {
                        MinLatestEnd = vertices[dependedTask.Key].LatestStart - dependedTask.Value.Lag + vertices[TaskName].Duration - 1;
                        vertices[TaskName].LF = vertices[dependedTask.Key].LS.AddDays(-dependedTask.Value.Lag + vertices[TaskName].Duration - 1);
                    }
                    else if (dependedTask.Value.Type == "FF" && vertices[dependedTask.Key].LatestEnd - dependedTask.Value.Lag < MinLatestEnd)
                    {
                        MinLatestEnd = vertices[dependedTask.Key].LatestEnd - dependedTask.Value.Lag;
                        vertices[TaskName].LF = vertices[dependedTask.Key].LF.AddDays(-dependedTask.Value.Lag);
                    }
                    else if (dependedTask.Value.Type == "FS" && vertices[dependedTask.Key].LatestStart - 1 - dependedTask.Value.Lag < MinLatestEnd)
                    {
                        MinLatestEnd = vertices[dependedTask.Key].LatestStart - 1 - dependedTask.Value.Lag;
                        vertices[TaskName].LF = vertices[dependedTask.Key].LS.AddDays(-1 - dependedTask.Value.Lag);
                    }
                    else if (dependedTask.Value.Type == "SF" && vertices[dependedTask.Key].LatestEnd - dependedTask.Value.Lag + vertices[TaskName].Duration - 1 < MinLatestEnd)
                    {
                        MinLatestEnd = vertices[dependedTask.Key].LatestEnd - dependedTask.Value.Lag + vertices[TaskName].Duration - 1;
                        vertices[TaskName].LF = vertices[dependedTask.Key].LF.AddDays(-dependedTask.Value.Lag + vertices[TaskName].Duration - 1);
                    }
                }

                vertices[TaskName].LatestEnd = MinLatestEnd;

                if (TaskName == "Start")
                {
                    vertices[TaskName].LatestStart = MinLatestEnd - vertices[TaskName].Duration;
                    vertices[TaskName].LS = vertices[TaskName].LF.AddDays(-vertices[TaskName].Duration);
                }
                else
                {
                    vertices[TaskName].LatestStart = MinLatestEnd - vertices[TaskName].Duration + 1;
                    vertices[TaskName].LS = vertices[TaskName].LF.AddDays(-vertices[TaskName].Duration + 1);
                }

                vertices[TaskName].TotalFloat = vertices[TaskName].LatestStart - vertices[TaskName].EarliestStart;
            }
        }

        private Queue<string> SortTopology()
        {
            Dictionary<string, int> verticesWithNumofIncomingEdges = new Dictionary<string, int>();
            Queue<string> sortedVertices = new Queue<string>();

            foreach (KeyValuePair<string, Vertex> vertex in vertices)
            {
                verticesWithNumofIncomingEdges.Add(vertex.Key, vertex.Value.Depending_vertices.Count);
            }

            while (verticesWithNumofIncomingEdges.Count > 0)
            {
                foreach (KeyValuePair<string, int> vertex in verticesWithNumofIncomingEdges)
                {
                    if (vertex.Value == 0)
                    {
                        verticesWithNumofIncomingEdges.Remove(vertex.Key);
                        sortedVertices.Enqueue(vertex.Key);

                        foreach (KeyValuePair<string, Vertex> v in vertices)
                        {
                            if (v.Key != vertex.Key && v.Value.Depending_vertices.ContainsKey(vertex.Key))
                            {
                                --verticesWithNumofIncomingEdges[v.Key];
                            }
                        }

                        break;
                    }
                }
            }

            return sortedVertices;
        }

        public void AddDurationTo(string vertex, int duration)
        {
            if (vertices.ContainsKey(vertex)) vertices[vertex].Duration = duration;
            else Print($"Cannot add duration to vertex {vertex} because it does not exist!\n");
        }

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
                    if (vertex.Value.Depended_vertices.ContainsKey(TaskName)) vertex.Value.Depended_vertices.Remove(TaskName);

                    if (vertex.Value.Depending_vertices.ContainsKey(TaskName)) vertex.Value.Depending_vertices.Remove(TaskName);
                }
            }
            else Print($"Cannot remove vertex {TaskName} because it does not exist!\n");
        }

        public void AddEdge(string v1, string v2, string Type, int Lag = 0)
        {
            AddVertex(v1);
            AddVertex(v2);

            if (!CheckIfEdgeExists(v1, v2))
            {
                vertices[v1].Depended_vertices.Add(v2, new TypeLag(Type));
                vertices[v2].Depending_vertices.Add(v1, new TypeLag(Type));
                vertices[v1].Depended_vertices[v2].Lag = Lag;
                vertices[v2].Depending_vertices[v1].Lag = Lag;
            }
            else
            {
                if (vertices[v1].Depended_vertices[v2].Type != Type)
                {
                    vertices[v1].Depended_vertices[v2].Type = Type;
                    vertices[v2].Depending_vertices[v1].Type = Type;
                }

                if (vertices[v1].Depended_vertices[v2].Lag != Lag)
                {
                    vertices[v1].Depended_vertices[v2].Lag = Lag;
                    vertices[v2].Depending_vertices[v1].Lag = Lag;
                }
            }
        }

        public void RemoveEdge(string v1, string v2)
        {
            if (CheckIfEdgeExists(v1, v2))
            {
                vertices[v1].Depended_vertices.Remove(v2);
                vertices[v2].Depending_vertices.Remove(v1);
            }
            else Print($"Cannot remove edge {v1} -> {v2} because it does not exist!\n");
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

        private bool CheckingLoop2(string expandVertex, List<string> visited)
        {
            if (visited.Contains(expandVertex))
            {
                Print($"The sequence of dependencies that creates loop: ");
                foreach (string vertex in visited)
                {
                    Print($"{vertex} -> ");
                }
                Print($"{expandVertex}\n");

                return true;
            }
            else
            {
                visited.Add(expandVertex);

                foreach (KeyValuePair<string, TypeLag> neighbor in vertices[expandVertex].Depended_vertices)
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

        public void PrintAllInfo()
        {
            if (Start.ES != DateTime.MinValue && Start.EF != DateTime.MinValue)
            {
                Print($"vertex: {Start.TaskName}\n");
                Print($"Duration = {Start.Duration}\n");
                Print($"{Start.TaskName}.ES = {Start.EarliestStart}\n");
                Print($"{Start.TaskName}.EF = {Start.EarliestEnd}\n");
                Print($"{Start.TaskName}.LS = {Start.LatestStart}\n");
                Print($"{Start.TaskName}.LF = {Start.LatestEnd}\n");
                Print($"Earliest Start Date: {Start.ES}\n");
                Print($"Earliest Finish Date: {Start.EF}\n");
                Print($"Latest Start Date: {Start.LS}\n");
                Print($"Latest Finish Date: {Start.LF}\n");
                Print($"Total Float = {Start.TotalFloat}\n");

                Print("Depending: ");
                foreach (KeyValuePair<string, TypeLag> v in Start.Depending_vertices)
                {
                    Print($"{v.Key}-{v.Value.Type}{v.Value.Lag} ");
                }

                Print("\nDepended: ");
                foreach (KeyValuePair<string, TypeLag> v in Start.Depended_vertices)
                {
                    Print($"{v.Key}-{v.Value.Type}{v.Value.Lag} ");
                }
                Print("\n\n");
            }

            foreach (KeyValuePair<string, Vertex> vertex in vertices)
            {
                Print($"vertex: {vertex.Key}\n");
                Print($"Duration = {vertex.Value.Duration}\n");
                Print($"{vertex.Key}.ES = {vertex.Value.EarliestStart}\n");
                Print($"{vertex.Key}.EF = {vertex.Value.EarliestEnd}\n");
                Print($"{vertex.Key}.LS = {vertex.Value.LatestStart}\n");
                Print($"{vertex.Key}.LF = {vertex.Value.LatestEnd}\n");
                Print($"Earliest Start Date: {vertex.Value.ES}\n");
                Print($"Earliest Finish Date: {vertex.Value.EF}\n");
                Print($"Latest Start Date: {vertex.Value.LS}\n");
                Print($"Latest Finish Date: {vertex.Value.LF}\n");
                Print($"Total Float = {vertex.Value.TotalFloat}\n");

                Print("Depending: ");
                foreach (KeyValuePair<string, TypeLag> v in vertex.Value.Depending_vertices)
                {
                    Print($"{v.Key}-{v.Value.Type}{v.Value.Lag} ");
                }

                Print("\nDepended: ");
                foreach (KeyValuePair<string, TypeLag> v in vertex.Value.Depended_vertices)
                {
                    Print($"{v.Key}-{v.Value.Type}{v.Value.Lag} ");
                }
                Print("\n\n");
            }

            if (End.LS != DateTime.MinValue && End.LF != DateTime.MinValue)
            {
                Print($"vertex: {End.TaskName}\n");
                Print($"Duration = {End.Duration}\n");
                Print($"{End.TaskName}.ES = {End.EarliestStart}\n");
                Print($"{End.TaskName}.EF = {End.EarliestEnd}\n");
                Print($"{End.TaskName}.LS = {End.LatestStart}\n");
                Print($"{End.TaskName}.LF = {End.LatestEnd}\n");
                Print($"Earliest Start Date: {End.ES}\n");
                Print($"Earliest Finish Date: {End.EF}\n");
                Print($"Latest Start Date: {End.LS}\n");
                Print($"Latest Finish Date: {End.LF}\n");
                Print($"Total Float = {End.TotalFloat}\n");

                Print("Depending: ");
                foreach (KeyValuePair<string, TypeLag> v in End.Depending_vertices)
                {
                    Print($"{v.Key}-{v.Value.Type}{v.Value.Lag} ");
                }

                Print("\nDepended: ");
                foreach (KeyValuePair<string, TypeLag> v in End.Depended_vertices)
                {
                    Print($"{v.Key}-{v.Value.Type}{v.Value.Lag} ");
                }
                Print("\n\n");
            }

            Queue<string> topo = SortTopology();

            Print("\n\nTopo sort: ");
            foreach (string s in topo)
            {
                Print($"{s} ");
            }
            Print("\n");
        }

        private void Print(string text)
        {
            Console.Write(text);
        }
    }

    class GanttChart
    {
        public PDMDiGraph Graph = new PDMDiGraph();
        private TreeOfTasks Tree;
        public List<string> TasksWithOrder = new List<string>();

        public GanttChart(TreeOfTasks Tree)
        {
            this.Tree = Tree;
            bool HasDependencies = true;

            if (Tree.Dependencies.Count == 0)
            {
                HasDependencies = false;
                Print("No dependencies found!\n");
            }

            List<string> TopoSort = new List<string>();

            if (HasDependencies)
            {
                BuildPDMGraph();
                TopoSort = TopoSortForChart();
            }

            Print("array of Tasks before adding subtasks without dependencies: ");
            foreach (string task in TopoSort)
            {
                Print($"{task} ");
            }
            Print("\n\n");

            LinkedList<string> tasksWithOrder = new LinkedList<string>(TopoSort);
            List<string> subtasksWithoutDependency = SubtasksWithoutDependency();

            foreach (string subtask in subtasksWithoutDependency)
            {
                AddSubtaskWithoutDependencyToTopo(subtask, tasksWithOrder, HasDependencies);
            }

            string[] array = new string[tasksWithOrder.Count];
            tasksWithOrder.CopyTo(array, 0);

            Print("array of Tasks after adding subtasks without dependencies: ");
            foreach (string task in array)
            {
                Print($"{task} ");
            }
            Print("\n\n");

            int level = FindLongestIDlength(tasksWithOrder) - 1;
            Dictionary<int, List<string>> levelWithTasks = LevelInTreeWithItsTasksExceptSubtasks();

            while (level > 0)
            {
                foreach (string task in levelWithTasks[level])
                {
                    array = AddSummaryTaskToTopo(task, array);

                    Print("tasks in array now is: ");
                    foreach (string tsk in array)
                    {
                        Print($"{tsk} ");
                    }
                    Print("\n\n");
                }

                --level;
            }

            TasksWithOrder = new List<string>(array);
        }

        private void BuildPDMGraph()
        {
            Graph = new PDMDiGraph();

            foreach (KeyValuePair<string, DependencyTaskInfo> dependency in Tree.Dependencies)
            {
                List<string> leafTasks = Tree.FindLeafTasksOf(dependency.Key);
                List<string> earliestLeafTasks = new List<string>();
                List<string> latestLeafTasks = new List<string>();

                if (leafTasks.Count == 1)
                {
                    earliestLeafTasks.Add(leafTasks[0]);
                    latestLeafTasks.Add(leafTasks[0]);
                }
                else
                {
                    foreach (string task in leafTasks)
                    {
                        if (!Tree.AlreadySettingTimeline(task))
                        {
                            Print($"Timeline of task {task} has not been set!\n");
                            Print($"Cannot create Gantt Chart!\n");
                            return;
                        }
                    }

                    earliestLeafTasks = Tree.EarliestTasks(leafTasks);
                    latestLeafTasks = Tree.LatestTasks(leafTasks);
                }

                foreach (KeyValuePair<string, TypeLag> depending in dependency.Value.DependingTasks)
                {
                    List<string> dependingLeafTasks = Tree.FindLeafTasksOf(depending.Key);
                    List<string> earliestDependingLeafTasks = new List<string>();
                    List<string> latestDependingLeafTasks = new List<string>();

                    string Type = depending.Value.Type;
                    int Lag = depending.Value.Lag;

                    if (dependingLeafTasks.Count == 1)
                    {
                        earliestDependingLeafTasks.Add(dependingLeafTasks[0]);
                        latestDependingLeafTasks.Add(dependingLeafTasks[0]);
                    }
                    else
                    {
                        foreach (string task in dependingLeafTasks)
                        {
                            if (!Tree.AlreadySettingTimeline(task))
                            {
                                Print($"Timeline of {task} has not been set!\n");
                                Print("Cannot create Gantt Chart!\n");
                                return;
                            }
                        }

                        earliestDependingLeafTasks = Tree.EarliestTasks(dependingLeafTasks);
                        latestDependingLeafTasks = Tree.LatestTasks(dependingLeafTasks);
                    }

                    if (Type == "SS")
                    {
                        foreach (string Task in earliestLeafTasks)
                        {
                            Task? TaskNode = Tree.FindTaskNode(Task);
                            if (TaskNode == null)
                            {
                                Print($"Cannot find task {Task} in Tree!\n");
                                Print("Cannot create Gantt Chart!\n");
                                return;
                            }

                            foreach (string DependingTask in earliestDependingLeafTasks)
                            {
                                Task? DependingTaskNode = Tree.FindTaskNode(DependingTask);
                                if (DependingTaskNode == null)
                                {
                                    Print($"Cannot find task {DependingTask} in Tree!\n");
                                    Print("Cannot create Gantt Chart!\n");
                                    return;
                                }

                                Graph.AddEdge(DependingTask, Task, "SS", Lag);
                                Graph.AddDurationTo(DependingTask, DependingTaskNode.Duration);
                            }

                            Graph.AddDurationTo(Task, TaskNode.Duration);
                        }
                    }
                    else if (Type == "FF")
                    {
                        foreach (string Task in latestLeafTasks)
                        {
                            Task? TaskNode = Tree.FindTaskNode(Task);
                            if (TaskNode == null)
                            {
                                Print($"Cannot find task {Task} in Tree!\n");
                                Print("Cannot create Gantt Chart!\n");
                                return;
                            }

                            foreach (string DependingTask in latestDependingLeafTasks)
                            {
                                Task? DependingTaskNode = Tree.FindTaskNode(DependingTask);
                                if (DependingTaskNode == null)
                                {
                                    Print($"Cannot find task {DependingTask} in Tree!\n");
                                    Print("Cannot create Gantt Chart!\n");
                                    return;
                                }

                                Graph.AddEdge(DependingTask, Task, "FF", Lag);
                                Graph.AddDurationTo(DependingTask, DependingTaskNode.Duration);
                            }

                            Graph.AddDurationTo(Task, TaskNode.Duration);
                        }
                    }
                    else if (Type == "FS")
                    {
                        foreach (string Task in earliestLeafTasks)
                        {
                            Task? TaskNode = Tree.FindTaskNode(Task);
                            if (TaskNode == null)
                            {
                                Print($"Cannot find task {Task} in Tree!\n");
                                Print("Cannot create Gantt Chart!\n");
                                return;
                            }

                            foreach (string DependingTask in latestDependingLeafTasks)
                            {
                                Task? DependingTaskNode = Tree.FindTaskNode(DependingTask);
                                if (DependingTaskNode == null)
                                {
                                    Print($"Cannot find task {DependingTask} in Tree!\n");
                                    Print("Cannot create Gantt Chart!\n");
                                    return;
                                }

                                Graph.AddEdge(DependingTask, Task, "FS", Lag);
                                Graph.AddDurationTo(DependingTask, DependingTaskNode.Duration);
                            }

                            Graph.AddDurationTo(Task, TaskNode.Duration);
                        }
                    }
                    else if (Type == "SF")
                    {
                        foreach (string Task in latestLeafTasks)
                        {
                            Task? TaskNode = Tree.FindTaskNode(Task);
                            if (TaskNode == null)
                            {
                                Print($"Cannot find task {Task} in Tree!\n");
                                Print("Cannot create Gantt Chart!\n");
                                return;
                            }

                            foreach (string DependingTask in earliestDependingLeafTasks)
                            {
                                Task? DependingTaskNode = Tree.FindTaskNode(DependingTask);
                                if (DependingTaskNode == null)
                                {
                                    Print($"Cannot find task {DependingTask} in Tree!\n");
                                    Print("Cannot create Gantt Chart!\n");
                                    return;
                                }

                                Graph.AddEdge(DependingTask, Task, "SF", Lag);
                                Graph.AddDurationTo(DependingTask, DependingTaskNode.Duration);
                            }

                            Graph.AddDurationTo(Task, TaskNode.Duration);
                        }
                    }

                    if (Graph.CheckingLoop())
                    {
                        Print("Cannot create Gantt Chart because of loop!\n");
                        return;
                    }
                }
            }

            foreach (KeyValuePair<string, Vertex> vertex in Graph.vertices)
            {
                if (vertex.Value.Depending_vertices.Count == 0)
                {
                    Task? Task = Tree.FindTaskNode(vertex.Key);
                    if (Task == null)
                    {
                        Print($"Cannot find task {vertex.Key} in Tree!\n");
                        return;
                    }

                    vertex.Value.ES = Task.StartDate;
                    vertex.Value.EF = Task.EndDate;
                }
            }
        }

        private string[] AddSummaryTaskToTopo(string TaskName, string[] array)
        {
            string TaskID = Tree.GetTaskID(TaskName);
            List<int> indices = new List<int>();
            bool repeat = false;

            for (int i = 0, plus = 0; i < array.Length; ++i)
            {
                string ID = Tree.GetTaskID(array[i]);

                if (ID.Length > TaskID.Length && TaskID == ID.Substring(0, TaskID.Length) && !repeat) //ID.Length > 1 && TaskID == ID.Substring(0, ID.Length - 1) && !repeat
                {
                    indices.Add(i + plus);
                    ++plus;
                    repeat = true;
                }
                else if (!(ID.Length > TaskID.Length && TaskID == ID.Substring(0, TaskID.Length) && repeat)) repeat = false;
                //ID.Length > 1 && TaskID == ID.Substring(0, ID.Length - 1) && repeat
            }

            string[] newArray = new string[array.Length + indices.Count];

            foreach (int index in indices)
            {
                newArray[index] = TaskName;
            }

            for (int i = 0, j = 0; j < array.Length; ++i, ++j)
            {
                if (newArray[i] == null) newArray[i] = array[j];
                else
                {
                    while (newArray[i] != null) ++i;
                    newArray[i] = array[j];
                }
            }

            return newArray;
        }

        private List<string> SubtasksWithoutDependency()
        {
            List<string> tasks = new List<string>();

            foreach (KeyValuePair<string, TaskIDandSubtasksID> ID in Tree.TaskIDandSubTaskIDDic)
            {
                string TaskName = Tree.GetTaskNameFromID(ID.Key);

                if (ID.Value.SubTaskIDS.Count == 0 && !Graph.vertices.ContainsKey(TaskName))
                {
                    tasks.Add(TaskName);
                }
            }

            return tasks;
        }

        private Dictionary<int, List<string>> LevelInTreeWithItsTasksExceptSubtasks()
        {
            Dictionary<int, List<string>> dic = new Dictionary<int, List<string>>();

            foreach (KeyValuePair<string, TaskIDandSubtasksID> ID in Tree.TaskIDandSubTaskIDDic)
            {
                if (ID.Value.SubTaskIDS.Count > 0 && ID.Key != "0")
                {
                    int IDlength = ID.Key.Length;
                    if (!dic.ContainsKey(IDlength)) dic.Add(IDlength, new List<string>());

                    dic[IDlength].Add(Tree.GetTaskNameFromID(ID.Key));
                }
            }

            return dic;
        }

        private void AddSubtaskWithoutDependencyToTopo(string TaskName, LinkedList<string> linkedList, bool HasDependencies = true)
        {
            string chosen = "";
            Task? Task = Tree.FindTaskNode(TaskName);
            if (Task == null)
            {
                Print($"Cannot add task {TaskName} to Topology sort because it does not exist!\n");
                return;
            }

            foreach (string s in linkedList)
            {
                if (HasDependencies && Task.StartDate < Graph.vertices[s].ES)
                {
                    chosen = s;
                    break;
                }
                else if (!HasDependencies)
                {
                    Task? linkedListTask = Tree.FindTaskNode(s);
                    if (linkedListTask == null)
                    {
                        Print($"Error! Task {s} is not in Tree!\n");
                        return;
                    }

                    if (Task.StartDate < linkedListTask.StartDate)
                    {
                        chosen = s;
                        break;
                    }
                }
            }

            if (chosen == "") linkedList.AddLast(TaskName);
            else
            {
                LinkedListNode<string>? current = linkedList.Find(chosen);
                if (current == null)
                {
                    Print($"Cannot find the chose task {chosen}!\n");
                    return;
                }
                else linkedList.AddBefore(current, TaskName);
            }
        }

        private int FindLongestIDlength(LinkedList<string> listOfTasks)
        {
            List<int> nums = new List<int>();

            foreach (string task in listOfTasks)
            {
                int length = Tree.GetTaskID(task).Length;

                if (!nums.Contains(length))
                {
                    nums.Add(length);
                }
            }

            return nums.Max();
        }

        private List<string> TopoSortForChart()
        {
            List<string> Sort = new List<string>();
            List<string> Tasks = new List<string>();

            foreach (KeyValuePair<string, Vertex> vertex in Graph.vertices)
            {
                if (vertex.Value.Depending_vertices.Count == 0)
                {
                    Tasks.Add(vertex.Key);
                }
            }

            string RecentTask = Tree.EarliestTasks(Tasks)[0];
            DateTime EarliestStart = Graph.vertices[RecentTask].ES;
            Dictionary<string, int> verticesWithNumofIncomingEdges = new Dictionary<string, int>();

            foreach (KeyValuePair<string, Vertex> vertex in Graph.vertices)
            {
                verticesWithNumofIncomingEdges.Add(vertex.Key, vertex.Value.Depending_vertices.Count);
            }

            verticesWithNumofIncomingEdges.Remove(RecentTask);

            foreach (KeyValuePair<string, Vertex> v in Graph.vertices)
            {
                if (v.Key != RecentTask && v.Value.Depending_vertices.ContainsKey(RecentTask))
                {
                    --verticesWithNumofIncomingEdges[v.Key];
                }
            }

            Sort.Add(RecentTask);

            while (verticesWithNumofIncomingEdges.Count > 0)
            {
                List<Vertex> verticesWithNoPredecessors = new List<Vertex>();

                foreach (KeyValuePair<string, int> vertex in verticesWithNumofIncomingEdges)
                {
                    if (vertex.Value == 0)
                    {
                        verticesWithNoPredecessors.Add(Graph.vertices[vertex.Key]);
                    }
                }

                string ChosenTask = ReturnTaskWithLowestHC(verticesWithNoPredecessors, RecentTask, EarliestStart);
                Sort.Add(ChosenTask);
                verticesWithNumofIncomingEdges.Remove(ChosenTask);
                RecentTask = ChosenTask;

                foreach (KeyValuePair<string, Vertex> v in Graph.vertices)
                {
                    if (v.Key != ChosenTask && v.Value.Depending_vertices.ContainsKey(ChosenTask))
                    {
                        --verticesWithNumofIncomingEdges[v.Key];
                    }
                }
            }

            return Sort;
        }

        private string ReturnTaskWithLowestHC(List<Vertex> vertices, string RecentTaskName, DateTime EarliestStartOfAll)
        {
            string Task = "";
            int minValue = int.MaxValue;

            foreach (Vertex vertex in vertices)
            {
                int value = CalculateHeuristic(vertex.TaskName, RecentTaskName, EarliestStartOfAll);

                if (value < minValue)
                {
                    minValue = value;
                    Task = vertex.TaskName;
                }
            }

            if (vertices.Count == 0) Print($"Warning! Return an empty task string!\n");
            return Task;
        }

        private int CalculateHeuristic(string TaskName, string RecentTaskName, DateTime EarliestStartOfAll)
        {
            string TaskID = Tree.GetTaskID(TaskName);
            string RecentTaskID = Tree.GetTaskID(RecentTaskName);
            if (TaskID == "" || RecentTaskID == "") Print($"Warning! Task {TaskName} or {RecentTaskName} does not exist!\n");

            int TaskIDtoInt = TaskID.Length == 1 ? 0 : int.Parse(TaskID.Substring(0, TaskID.Length - 1));
            int RecentTaskIDtoInt = RecentTaskID.Length == 1 ? 0 : int.Parse(RecentTaskID.Substring(0, RecentTaskID.Length - 1));

            TimeSpan span = Graph.vertices[TaskName].ES - EarliestStartOfAll;
            int value = Math.Abs(TaskIDtoInt - RecentTaskIDtoInt) + span.Days;
            return value;
        }

        private void Print(string text)
        {
            Console.Write(text);
        }
    }

    class Program
    {
        static void Main()
        {
            TreeOfTasks Tree = new TreeOfTasks("Project");

            Tree.AddTaskToRootTask("A");
            Tree.AddTaskToRootTask("B");
            Tree.AddTaskToRootTask("C");

            Tree.AddSubtaskToTask("A1", "A");
            Tree.AddSubtaskToTask("A2", "A");
            Tree.AddSubtaskToTask("A3", "A");

            Tree.AddSubtaskToTask("A11", "A1");
            Tree.AddSubtaskToTask("A12", "A1");

            Tree.AddSubtaskToTask("A31", "A3");
            Tree.AddSubtaskToTask("A32", "A3");
            Tree.AddSubtaskToTask("A33", "A3");

            Tree.AddSubtaskToTask("B1", "B");
            Tree.AddSubtaskToTask("B2", "B");

            Tree.AddSubtaskToTask("B21", "B2");
            Tree.AddSubtaskToTask("B22", "B2");

            Tree.AddSubtaskToTask("C1", "C");
            Tree.AddSubtaskToTask("C2", "C");
            Tree.AddSubtaskToTask("C3", "C");
            Tree.AddSubtaskToTask("C4", "C");

            Tree.SetTimelineForTask("A11", new DateTime(2024, 10, 10), new DateTime(2024, 10, 12));
            Tree.SetTimelineForTask("A12", new DateTime(2024, 10, 11), new DateTime(2024, 10, 12));
            Tree.SetTimelineForTask("A2", new DateTime(2024, 10, 10), new DateTime(2024, 10, 10));
            Tree.SetTimelineForTask("A31", new DateTime(2024, 10, 13), new DateTime(2024, 10, 17));
            Tree.SetTimelineForTask("A32", new DateTime(2024, 10, 15), new DateTime(2024, 10, 18));
            Tree.SetTimelineForTask("A33", new DateTime(2024, 10, 18), new DateTime(2024, 10, 19));

            Tree.SetTimelineForTask("B1", new DateTime(2024, 10, 21), new DateTime(2024, 10, 23));
            Tree.SetTimelineForTask("B21", new DateTime(2024, 10, 25), new DateTime(2024, 10, 29));
            Tree.SetTimelineForTask("B22", new DateTime(2024, 10, 29), new DateTime(2024, 10, 31));

            Tree.SetTimelineForTask("C1", new DateTime(2024, 10, 22), new DateTime(2024, 10, 23));
            Tree.SetTimelineForTask("C2", new DateTime(2024, 10, 24), new DateTime(2024, 10, 28));
            Tree.SetTimelineForTask("C3", new DateTime(2024, 10, 27), new DateTime(2024, 10, 31));
            Tree.SetTimelineForTask("C4", new DateTime(2024, 10, 31), new DateTime(2024, 10, 31));

            //Tree.PrintAllTasksInfo();

            Tree.AddDependency("A12", "A11", "FF");
            Tree.AddDependency("A2", "A11", "SS");
            Tree.AddDependency("A12", "A2", "SF");
            Tree.AddDependency("A31", "A12", "FS");
            Tree.AddDependency("A32", "A31", "FF");
            Tree.AddDependency("A33", "A32", "FS");

            Tree.AddDependency("B", "A", "FS");

            Tree.AddDependency("C", "B", "FF");

            Tree.AddDependency("C2", "C1", "SS");
            Tree.AddDependency("C3", "C2", "FS");
            Tree.AddDependency("C4", "C3", "FF");

            //Print("\n\n");
            //Tree.PrintAllTasksInfo();

            Tree.AddLagToDependency("A12", "A2", 2);
            Tree.AddLagToDependency("A32", "A31", 1);
            Tree.AddLagToDependency("A33", "A32", -1);
            Tree.AddLagToDependency("B", "A", 1);
            Tree.AddLagToDependency("C2", "C1", 2);
            Tree.AddLagToDependency("C3", "C2", -2);

            Tree.UpdateStatus("A11", "In progress");
            Tree.UpdateStatus("A2", "Complete");
            Tree.UpdateStatus("A12", "Complete");
            Tree.UpdateStatus("A11", "Complete");
            Tree.UpdateStatus("A31", "In progress");
            Tree.UpdateStatus("A32", "In progress");
            Tree.UpdateStatus("A31", "Complete");
            Tree.UpdateStatus("A32", "Complete");

            Print("\n\n");
            Tree.PrintAllTasksInfo();

            GanttChart Chart = new GanttChart(Tree);
        }

        static void Print(string text)
        {
            Console.Write(text);
        }
    }
}