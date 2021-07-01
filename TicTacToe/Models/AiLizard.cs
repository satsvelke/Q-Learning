﻿using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ai_lizard_ui.Models
{
    public class AiLizard : Hub
    {
        public void Train(int totalEpisodes, string enviormentPath)
        {
            var columns = 6; var rows = 6;

            var q = new Q();
            var exploration_rate_threshhold = 0.0M;

            var env = new List<QStates>();

            if (enviormentPath.Equals("na"))
                env = q.GetEnviorment(columns, rows); /// get enviorment with state values 
            else
            {
                var savedEnviorment = File.ReadAllText(string.Concat(@"d:\\qlog\\enviorments\\", enviormentPath));
                env = JsonConvert.DeserializeObject<List<QStates>>(savedEnviorment);
            }

            var logfile = @"d:\\qlog\\episode_log_" + DateTime.Now.ToString("ddmmyyyyhhsstt") + ".txt";
            var savedEnv = @"d:\\qlog\\enviorments\\env_" + DateTime.Now.ToString("ddmmyyyyhhsstt") + ".txt";
            var uniqueStateList = new List<QStates>();
            var nextState = new QStates();
            var currentState = new QStates();
            Clients.All.GetPath(env);

            var finalUniquePaths = new List<string>();

            // episodes loop 
            for (int episode = 1; episode < totalEpisodes; episode++)
            {
                currentState = env.Where(c => c.PositionName == string.Concat("L1")).FirstOrDefault();

                for (int i = 0; i < 36; i++)
                {
                    // System.Threading.Thread.Sleep(200);

                    // get random initial action 
                    var randomAction = q.GetRandomAction(columns, rows);
                    var actionValue = randomAction.FirstOrDefault().Value;

                    // Clients.All.CurrentState(currentState);


                    if ((currentState.PositionName == "L7"
                        || currentState.PositionName == "L13"
                        || currentState.PositionName == "L19"
                        || currentState.PositionName == "L25"
                        || currentState.PositionName == "31") && actionValue == -1)
                    {
                        // nextPosition = currentPosition;
                        continue;
                    }
                    else
                    {
                        exploration_rate_threshhold = Convert.ToDecimal(new Random().NextDouble());

                        if (exploration_rate_threshhold > q.ExplorationRate)
                            nextState = env.Where(c => c.QValue == (q.GetMaxQStateFromCurrentPostion(currentState, env)).QValue).FirstOrDefault();
                        else
                            nextState = env.Where(c => c.PositionId == (currentState.PositionId + actionValue)).FirstOrDefault();
                    }

                    Clients.All.NextState(nextState);

                    if (nextState == null)
                        continue;

                    var newQValue = q.GetQValue(nextState, env);



                    var updatedEnv = env.FirstOrDefault(x => x.PositionName == nextState.PositionName);
                    if (updatedEnv != null)
                        updatedEnv.QValue = newQValue;


                    q.CurrentQ = newQValue;

                    currentState = nextState;

                    if (nextState.Reward == -100M || nextState.Reward == 100M)
                        break;


                }


                q.ExplorationRate = q.MinimumExplorationRate + (q.MaxExplorationRate - q.MinimumExplorationRate) * Convert.ToDecimal(Math.Exp((double)(-(q.ExplorationDecayRate * episode))));
                //q.ExplorationRate = Math.Max(q.MinimumExplorationRate, (q.ExplorationRate * q.ExplorationDecayRate));
                //if (q.ExplorationRate >= q.MinimumExplorationRate)
                //    q.ExplorationRate *= q.ExplorationDecayRate;

                Clients.All.GetExplorationRate(q.ExplorationRate);

                string r = string.Empty;
                string path = "L1,";

                var cenp = env.Where(c => c.PositionName == string.Concat("L1")).FirstOrDefault();
                for (int i = 0; i < 36; i++)
                {
                    var maxQstate = q.GetMaxQStateFromCurrentPostion(cenp, env);

                    r += maxQstate.PositionName + "-->";
                    path += maxQstate.PositionName + ",";
                    cenp = maxQstate;
                    if (cenp.Reward == 100)
                    { break; }
                }

                path = path.TrimEnd(',');
                File.AppendAllText(logfile, "=>" + r + Environment.NewLine);

                if (!finalUniquePaths.Contains(path))
                    finalUniquePaths.Add(path);


                Clients.All.GetPath(env);
                Clients.All.GetEpisode(episode);
            }/// episode loop 


            File.AppendAllText(logfile, "Final Unique Path" + Environment.NewLine);
            foreach (var p in finalUniquePaths)
                File.AppendAllText(logfile, p + Environment.NewLine);

            Clients.All.UniqueState(finalUniquePaths, Path.GetFileName(savedEnv));
            File.AppendAllText(savedEnv, JsonConvert.SerializeObject(env));

        }

        public void FetchHighRewardPath(string enviormentPath)
        {
            var savedEnviorment = File.ReadAllText(string.Concat(@"d:\\qlog\\enviorments\\", enviormentPath));
            var enviorments = JsonConvert.DeserializeObject<List<QStates>>(savedEnviorment);

            string path = "L1,";
            var cenp = enviorments.Where(c => c.PositionName == string.Concat("L1")).FirstOrDefault();

            foreach (var env in enviorments)
            {
                var maxQstate = new Q().GetMaxQStateFromCurrentPostion(cenp, enviorments);
                path += maxQstate.PositionName + ",";
                cenp = maxQstate;
            }
           
            Clients.All.GetHighRewardPath(path.TrimEnd(','));
        }
    }

    
    public class Q
    {
        public decimal CurrentQ { get; set; } = 0;
        public decimal LearningRate { get; set; } = 0.2M; // 
        public decimal DiscountRate { get; set; } = 0.9M; // range from 0.8 to 0.99
        public decimal MaxQ { get; set; } = 0; // max reward achieved 
        public decimal CurrentQValues { get; set; } = 0;// current q value of block 
        public decimal ExplorationRate { get; set; } = 1M;
        public decimal MaxExplorationRate { get; set; } = 1M;
        public decimal MinimumExplorationRate { get; set; } = 0.005M;
        public decimal ExplorationDecayRate { get; set; } = 0.0000001M;

        public Dictionary<string, int> GetRandomAction(int columns, int rows, string key = "")
        {
            var leftPoints = -1;
            var rightPoints = 1;
            var up = -columns;
            var downPoints = columns;

            var actionsDictionary = new Dictionary<string, int>();
            actionsDictionary.Add("u", up);
            actionsDictionary.Add("d", downPoints);
            actionsDictionary.Add("l", leftPoints);
            actionsDictionary.Add("r", rightPoints);

            List<string> keyList = new List<string>(actionsDictionary.Keys);
            string randomKey = keyList[new Random().Next(keyList.Count)];

            var newDict = new Dictionary<string, int>();


            if (!string.IsNullOrWhiteSpace(key))
            {
                newDict.Add(key, actionsDictionary[key]);
                return newDict;
            }

            newDict.Add(randomKey, actionsDictionary[randomKey]);

            return newDict;
        }


        public QStates GetMaxQStateFromCurrentPostion(QStates qStates, List<QStates> env)
        {
            var columns = 6; var rows = 6;
            var listOfQValues = new List<QStates>();

            var u = GetRandomAction(columns, rows, "u").FirstOrDefault().Value;
            var r = GetRandomAction(columns, rows, "r").FirstOrDefault().Value;
            var d = GetRandomAction(columns, rows, "d").FirstOrDefault().Value;
            var l = GetRandomAction(columns, rows, "l").FirstOrDefault().Value;

            var uPosition = env.Where(c => c.PositionId == (qStates.PositionId + u)).FirstOrDefault();
            var rPosition = env.Where(c => c.PositionId == (qStates.PositionId + r)).FirstOrDefault();
            var dPosition = env.Where(c => c.PositionId == (qStates.PositionId + d)).FirstOrDefault();


            var lPosition = new QStates();
            if ((qStates.PositionName == "L7"
                       || qStates.PositionName == "L13"
                       || qStates.PositionName == "L19"
                       || qStates.PositionName == "L25"
                       || qStates.PositionName == "L31") && l == -1)
                lPosition = null;
            else
                lPosition = env.Where(c => c.PositionId == (qStates.PositionId + l)).FirstOrDefault();


            if (uPosition != null)
                listOfQValues.Add(uPosition);

            if (rPosition != null)
                listOfQValues.Add(rPosition);

            if (dPosition != null)
                listOfQValues.Add(dPosition);

            if (lPosition != null)
                listOfQValues.Add(lPosition);


            // maxQvalue = listOfQValues.Max();
            //     var pp = listOfQValues.OrderByDescending(c => c.QValue).First();
            return listOfQValues.OrderByDescending(c => c.QValue).First();
        }

        public decimal GetQValue(QStates qStates, List<QStates> env)
        {
            if (qStates != null)
            {

                var maxQState = GetMaxQStateFromCurrentPostion(qStates, env);
                //var learnedValue = qStates.Reward + (DiscountRate * maxQState.QValue);
                //var q = ((1 - LearningRate) * qStates.QValue) + (LearningRate * learnedValue);
                // var q = qStates.QValue * (1 - LearningRate) + LearningRate * (qStates.Reward + DiscountRate * maxQvalue);
                var q = qStates.QValue + LearningRate * (qStates.Reward + DiscountRate * (maxQState.QValue - qStates.QValue));
                return q;
            }
            else return 0;

        }

        public List<QStates> GetEnviorment(int rows, int columns)
        {
            var states = new List<QStates>();
            for (int i = 1; i < (rows * columns) + 1; i++)
            {
                var positionName = string.Concat("L", i);
                int reward;
                // set rewards 
                if (positionName.Equals("L2")) reward = -100;
                else if (positionName.Equals("L8")) reward = -100;
                else if (positionName.Equals("L14")) reward = -100;
                else if (positionName.Equals("L20")) reward = -100;
                else if (positionName.Equals("L26")) reward = -100;

                else if (positionName.Equals("L10")) reward = -100;
                //else if (positionName.Equals("L16")) reward = -100;
                else if (positionName.Equals("L22")) reward = -100;
                else if (positionName.Equals("L28")) reward = -100;
                else if (positionName.Equals("L35")) reward = -100;
                else if (positionName.Equals("L34")) reward = -100;

                else if (positionName.Equals("L11")) reward = -100;
                else if (positionName.Equals("L24")) reward = -100;



                else if (positionName.Equals("L36")) reward = 100;

                else reward = -1;
                states.Add(new QStates() { PositionId = i, QValue = 0, PositionName = positionName, Reward = reward });
            }


            return states;
        }
    }
    public class QStates
    {
        public int PositionId { get; set; }
        public string PositionName { get; set; }
        public decimal QValue { get; set; }
        public decimal Reward { get; set; }
    }
    public class QTable
    {
        public List<QStates> QStates { get; set; }
    }
}