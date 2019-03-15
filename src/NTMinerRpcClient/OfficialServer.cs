﻿using NTMiner.Controllers;
using NTMiner.MinerServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace NTMiner {
    public static class OfficialServer {
        public static readonly FileUrlServiceFace FileUrlService = FileUrlServiceFace.Instance;
        public static readonly OverClockDataServiceFace OverClockDataService = OverClockDataServiceFace.Instance;

        public static readonly string OfficialServerHost = "localhost";

        public static void PostAsync<T>(string controller, string action, object param, Action<T, Exception> callback) where T : class {
            Task.Factory.StartNew(() => {
                try {
                    using (HttpClient client = new HttpClient()) {
                        Task<HttpResponseMessage> message =
                            client.PostAsJsonAsync($"http://{OfficialServerHost}:{WebApiConst.MinerServerPort}/api/{controller}/{action}", param);
                        T response = message.Result.Content.ReadAsAsync<T>().Result;
                        callback?.Invoke(response, null);
                    }
                }
                catch (Exception e) {
                    callback?.Invoke(null, e);
                }
            });
        }

        private static T Post<T>(string controller, string action, object param) where T : class {
            try {
                using (HttpClient client = new HttpClient()) {
                    Task<HttpResponseMessage> message = client.PostAsJsonAsync($"http://{OfficialServerHost}:{WebApiConst.MinerServerPort}/api/{controller}/{action}", param);
                    T response = message.Result.Content.ReadAsAsync<T>().Result;
                    return response;
                }
            }
            catch {
                return null;
            }
        }

        private static void GetAsync<T>(string controller, string action, Dictionary<string, string> param, Action<T, Exception> callback) {
            Task.Factory.StartNew(() => {
                try {
                    using (HttpClient client = new HttpClient()) {
                        string queryString = string.Empty;
                        if (param != null && param.Count != 0) {
                            queryString = "?" + string.Join("&", param.Select(a => a.Key + "=" + a.Value));
                        }

                        Task<HttpResponseMessage> message =
                            client.GetAsync($"http://{OfficialServerHost}:{WebApiConst.MinerServerPort}/api/{controller}/{action}{queryString}");
                        T response = message.Result.Content.ReadAsAsync<T>().Result;
                        callback?.Invoke(response, null);
                    }
                }
                catch (Exception e) {
                    callback?.Invoke(default(T), e);
                }
            });
        }

        public static void GetTimeAsync(Action<DateTime> callback) {
            GetAsync("AppSetting", "GetTime", null, callback: (DateTime datetime, Exception e) => {
                callback?.Invoke(datetime);
                if (e != null) {
                    Logger.ErrorDebugLine($"GetTimeAsync失败 {e?.Message}");
                }
            });
        }

        public static void GetJsonFileVersionAsync(string key, Action<string> callback) {
            AppSettingRequest request = new AppSettingRequest {
                MessageId = Guid.NewGuid(),
                Key = key
            };
            PostAsync("AppSetting", "AppSetting", request, (string jsonFileVersion, Exception e) => {
                callback?.Invoke(jsonFileVersion);
                if (e != null) {
                    Logger.ErrorDebugLine($"GetJsonFileVersionAsync({AssemblyInfo.ServerJsonFileName})失败 {e?.Message}");
                }
            });
        }

        public class FileUrlServiceFace {
            public static readonly FileUrlServiceFace Instance = new FileUrlServiceFace();
            private static readonly string SControllerName = ControllerUtil.GetControllerName<IFileUrlController>();

            private FileUrlServiceFace() { }

            #region GetNTMinerUrlAsync
            // ReSharper disable once InconsistentNaming
            public void GetNTMinerUrlAsync(string fileName, Action<string, Exception> callback) {
                NTMinerUrlRequest request = new NTMinerUrlRequest {
                    FileName = fileName
                };
                PostAsync(SControllerName, nameof(IFileUrlController.NTMinerUrl), request, callback);
            }
            #endregion

            #region GetNTMinerFilesAsync
            // ReSharper disable once InconsistentNaming
            public void GetNTMinerFilesAsync(Action<List<NTMinerFileData>, Exception> callback) {
                PostAsync(SControllerName, nameof(IFileUrlController.NTMinerFiles), null, callback);
            }
            #endregion

            #region AddOrUpdateNTMinerFileAsync
            // ReSharper disable once InconsistentNaming
            public void AddOrUpdateNTMinerFileAsync(NTMinerFileData entity, Action<ResponseBase, Exception> callback) {
                DataRequest<NTMinerFileData> request = new DataRequest<NTMinerFileData>() {
                    Data = entity,
                    LoginName = SingleUser.LoginName
                };
                request.SignIt(SingleUser.PasswordSha1);
                PostAsync(SControllerName, nameof(IFileUrlController.AddOrUpdateNTMinerFile), request, callback);
            }
            #endregion

            #region RemoveNTMinerFileAsync
            // ReSharper disable once InconsistentNaming
            public void RemoveNTMinerFileAsync(Guid id, Action<ResponseBase, Exception> callback) {
                DataRequest<Guid> request = new DataRequest<Guid>() {
                    LoginName = SingleUser.LoginName,
                    Data = id
                };
                request.SignIt(SingleUser.PasswordSha1);
                PostAsync(SControllerName, nameof(IFileUrlController.RemoveNTMinerFile), request, callback);
            }
            #endregion

            #region GetLiteDbExplorerUrlAsync
            public void GetLiteDbExplorerUrlAsync(Action<string, Exception> callback) {
                PostAsync(SControllerName, nameof(IFileUrlController.LiteDbExplorerUrl), null, callback);
            }
            #endregion

            #region GetNTMinerUpdaterUrlAsync
            // ReSharper disable once InconsistentNaming
            public void GetNTMinerUpdaterUrlAsync(Action<string, Exception> callback) {
                PostAsync(SControllerName, nameof(IFileUrlController.NTMinerUpdaterUrl), null, callback);
            }
            #endregion

            #region GetPackageUrlAsync
            public void GetPackageUrlAsync(string package, Action<string, Exception> callback) {
                PackageUrlRequest request = new PackageUrlRequest {
                    Package = package
                };
                PostAsync(SControllerName, nameof(IFileUrlController.PackageUrl), request, callback);
            }
            #endregion
        }

        public class OverClockDataServiceFace {
            public static readonly OverClockDataServiceFace Instance = new OverClockDataServiceFace();
            private static readonly string SControllerName = ControllerUtil.GetControllerName<IOverClockDataController>();

            private OverClockDataServiceFace() { }

            #region GetOverClockDatas
            /// <summary>
            /// 同步方法
            /// </summary>
            /// <param name="messageId"></param>
            /// <returns></returns>
            public DataResponse<List<OverClockData>> GetOverClockDatas(Guid messageId) {
                try {
                    OverClockDatasRequest request = new OverClockDatasRequest {
                        MessageId = Guid.NewGuid()
                    };
                    DataResponse<List<OverClockData>> response = Post<DataResponse<List<OverClockData>>>(SControllerName, nameof(IOverClockDataController.OverClockDatas), request);
                    return response;
                }
                catch (Exception e) {
                    Logger.ErrorDebugLine(e.Message, e);
                    return null;
                }
            }
            #endregion

            #region AddOrUpdateOverClockDataAsync
            public void AddOrUpdateOverClockDataAsync(OverClockData entity, Action<ResponseBase, Exception> callback) {
                DataRequest<OverClockData> request = new DataRequest<OverClockData>() {
                    LoginName = SingleUser.LoginName,
                    Data = entity
                };
                request.SignIt(SingleUser.PasswordSha1);
                PostAsync(SControllerName, nameof(IOverClockDataController.AddOrUpdateOverClockData), request, callback);
            }
            #endregion

            #region RemoveOverClockDataAsync
            public void RemoveOverClockDataAsync(Guid id, Action<ResponseBase, Exception> callback) {
                DataRequest<Guid> request = new DataRequest<Guid>() {
                    LoginName = SingleUser.LoginName,
                    Data = id
                };
                request.SignIt(SingleUser.PasswordSha1);
                PostAsync(SControllerName, nameof(IOverClockDataController.RemoveOverClockData), request, callback);
            }
            #endregion
        }
    }
}