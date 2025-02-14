﻿using NTMiner.Report;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NTMiner.Core.Redis.Impl {
    public class SpeedDataRedis : ISpeedDataRedis {
        protected const string _redisKeySpeedDataByClientId = RedisKeyword.SpeedDatasSpeedDataByClientId;

        protected readonly IRedis _redis;
        public SpeedDataRedis(IRedis redis) {
            _redis = redis;
        }

        public Task<Dictionary<Guid, SpeedData>> GetAllAsync() {
            var db = _redis.RedisConn.GetDatabase();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            return db.HashGetAllAsync(_redisKeySpeedDataByClientId).ContinueWith(t => {
                stopwatch.Stop();
                string text = $"{nameof(SpeedDataRedis)}的redis方法HashGetAllAsync耗时 {stopwatch.GetElapsedSeconds().ToString("f2")} 秒";
                NTMinerConsole.UserInfo(text);
                stopwatch.Restart();
                Dictionary<Guid, SpeedData> dic = new Dictionary<Guid, SpeedData>();
                foreach (var item in t.Result) {
                    if (item.Value.HasValue) {
                        SpeedData data = VirtualRoot.JsonSerializer.Deserialize<SpeedData>(item.Value);
                        if (data != null) {
                            dic.Add(data.ClientId, data);
                        }
                    }
                }
                stopwatch.Stop();
                NTMinerConsole.UserInfo($"反序列化和装配SpeedData列表耗时 {stopwatch.GetElapsedSeconds().ToString("f2")} 秒");
                return dic;
            });
        }

        public Task<SpeedData> GetByClientIdAsync(Guid clientId) {
            if (clientId == Guid.Empty) {
                return Task.FromResult<SpeedData>(null);
            }
            var db = _redis.RedisConn.GetDatabase();
            return db.HashGetAsync(_redisKeySpeedDataByClientId, clientId.ToString()).ContinueWith(t => {
                if (t.Result.HasValue) {
                    return VirtualRoot.JsonSerializer.Deserialize<SpeedData>(t.Result);
                }
                else {
                    return null;
                }
            });
        }

        public Task SetAsync(SpeedData speedData) {
            if (speedData == null || speedData.ClientId == Guid.Empty) {
                return TaskEx.CompletedTask;
            }
            var db = _redis.RedisConn.GetDatabase();
            return db.HashSetAsync(_redisKeySpeedDataByClientId, speedData.ClientId.ToString(), VirtualRoot.JsonSerializer.Serialize(speedData));
        }

        public Task DeleteByClientIdAsync(Guid clientId) {
            if (clientId == Guid.Empty) {
                return TaskEx.CompletedTask;
            }
            var db = _redis.RedisConn.GetDatabase();
            return db.HashDeleteAsync(_redisKeySpeedDataByClientId, clientId.ToString());
        }
    }
}
