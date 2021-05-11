using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NetSDKCS;
using DHRealLoad.Models;
using System.Runtime.InteropServices;
using DotNetCore.CAP;
using System.IO;

namespace DHRealLoad.Controllers
{
    [Route("api/DHServer")]
    [ApiController]
    public class DHServerController : ControllerBase
    {
        private static IntPtr loginID = IntPtr.Zero;
        private static IntPtr realLoadID = IntPtr.Zero;
        private static fDisConnectCallBack disConnectCallBack = new fDisConnectCallBack(DisConnectCallBack);
        private static fHaveReConnectCallBack haveReConnectCallBack = new fHaveReConnectCallBack(ReConnectCallBack);
        private static fAnalyzerDataCallBack analyzerDataCallBack = new fAnalyzerDataCallBack(AnalyzerDataCallBack);
        private static NET_DEVICEINFO_Ex device;

        private readonly ILogger<DHServerController> _logger;
        private readonly ICapPublisher _capPublisher;
        public DHServerController(ICapPublisher publisher, ILogger<DHServerController> logger)
        {
            _capPublisher = publisher;
            _logger = logger;
        }
        /// <summary>
        /// 登录与登出设备
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        [HttpPost("LoginOrLoginOut")]
        public Result Post([FromBody] LoginParam param)
        {
            Result result = new Result();
            //初始化
            NETClient.Init(disConnectCallBack, IntPtr.Zero, null);
            if (IntPtr.Zero == loginID)
            {
                ushort port = 0;
                try
                {
                    port = Convert.ToUInt16(param.wDVRPort);
                }
                catch
                {
                    result.Message = "Input port error";
                    return result;
                }
                device = new NET_DEVICEINFO_Ex();
                loginID = NETClient.Login(param.pchDVRIP, port, param.pchUserName, param.pchPassword, EM_LOGIN_SPAC_CAP_TYPE.TCP, IntPtr.Zero, ref device);
                if (IntPtr.Zero == loginID)
                {
                    result.Message = NETClient.GetLastError();
                    return result;
                }
                else
                {
                    result.Success = true;
                    result.Message = "登录成功";
                    return result;
                }
            }
            else
            {
                if (realLoadID != IntPtr.Zero)
                {
                    NETClient.StopLoadPic(realLoadID);
                    realLoadID = IntPtr.Zero;
                }
                bool lo = NETClient.Logout(loginID);
                if (!lo)
                {
                    result.Message = NETClient.GetLastError();
                    return result;
                }
                loginID = IntPtr.Zero;
                NETClient.Cleanup();
                result.Success = true;
                result.Message = "登出成功";
                return result;
            }
        }
        /// <summary>
        /// 订阅/停止订阅智能事件
        /// </summary>
        [HttpGet("LoadPicture")]
        public void Get()
        {
            if (realLoadID == IntPtr.Zero)
            {
               // fAnalyzerDataCallBack cb=new fAnalyzerDataCallBack((IntPtr lAnalyzerHandle, uint dwEventType, IntPtr pEventInfo, IntPtr pBuffer, uint dwBufSize, IntPtr dwUser, int nSequence, IntPtr reserved) => AnalyzerDataCallBack(IntPtr lAnalyzerHandle, uint dwEventType, IntPtr pEventInfo, IntPtr pBuffer, uint dwBufSize, IntPtr dwUser, int nSequence, IntPtr reserved, ICapPublisher publisher));
                realLoadID = NETClient.RealLoadPicture(loginID, 0, (uint)EM_EVENT_IVS_TYPE.ALL, true, analyzerDataCallBack, IntPtr.Zero, IntPtr.Zero);
            }
            else
            {
                NETClient.StopLoadPic(realLoadID);
                realLoadID = IntPtr.Zero;
            }
        }
        /// <summary>
        /// 退出登录
        /// </summary>
        [HttpGet("LogOut")]
        public void LogOut()
        {
            if (loginID != IntPtr.Zero)
            {
                NETClient.Logout(loginID);
                loginID = IntPtr.Zero;
            }
        }
        /// <summary>
        /// 清理初始化资源
        /// </summary>
        [HttpGet("CleanUp")]
        public void CleanUp()
        {
            NETClient.Cleanup();
        }
        private static int AnalyzerDataCallBack(IntPtr lAnalyzerHandle, uint dwEventType, IntPtr pEventInfo, IntPtr pBuffer, uint dwBufSize, IntPtr dwUser, int nSequence, IntPtr reserved)
            //, ICapPublisher publisher)
        {
            DHServer dhs = new DHServer();
            dhs.nDVRType = device.nDVRType;
            dhs.sSerialNumber = device.sSerialNumber;
            switch (dwEventType)
            {
                // 警戒线事件
                case (uint)EM_EVENT_IVS_TYPE.CROSSLINEDETECTION:
                    {
                        dhs.dwEventType = EM_EVENT_IVS_TYPE.CROSSLINEDETECTION;
                        NET_DEV_EVENT_CROSSLINE_INFO info = (NET_DEV_EVENT_CROSSLINE_INFO)Marshal.PtrToStructure(pEventInfo, typeof(NET_DEV_EVENT_CROSSLINE_INFO));
                        dhs.nChannelID = info.nChannelID;
                        dhs.szName = info.szName;
                        dhs.UTC = info.UTC.ToShortString();
                        dhs.nEventID = info.nEventID;
                        dhs.byImageIndex = info.byImageIndex;
                        List<string> imgs = new List<string>();
                        //保存全景图
                        if (info.stuSceneImage.nLength > 0)
                        {
                            string pic_name = String.Format("./image/{0}_", info.nEventID);
                            pic_name = pic_name + "全景图.jpg";
                            byte[] bytes = new byte[info.stuSceneImage.nLength];
                            Marshal.Copy(new IntPtr(pBuffer.ToInt32() + info.stuSceneImage.nOffSet), bytes, 0, (int)info.stuSceneImage.nLength);
                            WriteFile(bytes, pic_name, (int)info.stuSceneImage.nLength);
                            imgs.Add(pic_name);
                        }
                        dhs.szFilePath = imgs;
                    }
                    break;
                // 警戒区事件
                case (uint)EM_EVENT_IVS_TYPE.CROSSREGIONDETECTION:
                    {
                        dhs.dwEventType = EM_EVENT_IVS_TYPE.CROSSREGIONDETECTION;
                        NET_DEV_EVENT_CROSSREGION_INFO info = (NET_DEV_EVENT_CROSSREGION_INFO)Marshal.PtrToStructure(pEventInfo, typeof(NET_DEV_EVENT_CROSSREGION_INFO));
                        dhs.nChannelID = info.nChannelID;
                        dhs.szName = info.szName;
                        dhs.UTC = info.UTC.ToShortString();
                        dhs.nEventID = info.nEventID;
                        dhs.byImageIndex = info.byImageIndex;
                        List<string> imgs = new List<string>();
                        //保存全景图
                        if (info.stuSceneImage.nLength > 0)
                        {
                            string pic_name = String.Format("./image/{0}_", info.nEventID);
                            pic_name = pic_name + "全景图.jpg";
                            byte[] bytes = new byte[info.stuSceneImage.nLength];
                            Marshal.Copy(new IntPtr(pBuffer.ToInt32() + info.stuSceneImage.nOffSet), bytes, 0, (int)info.stuSceneImage.nLength);
                            WriteFile(bytes, pic_name, (int)info.stuSceneImage.nLength);
                            imgs.Add(pic_name);
                        }
                        dhs.szFilePath = imgs;
                    }
                    break;
                // 物品遗留事件
                case (uint)EM_EVENT_IVS_TYPE.LEFTDETECTION:
                    {
                        dhs.dwEventType = EM_EVENT_IVS_TYPE.LEFTDETECTION;
                        NET_DEV_EVENT_LEFT_INFO info = (NET_DEV_EVENT_LEFT_INFO)Marshal.PtrToStructure(pEventInfo, typeof(NET_DEV_EVENT_LEFT_INFO));
                        dhs.nChannelID = info.nChannelID;
                        dhs.szName = info.szName;
                        dhs.UTC = info.UTC.ToShortString();
                        dhs.nEventID = info.nEventID;
                        dhs.byImageIndex = info.byImageIndex;
                    }
                    break;
                //物品搬移事件
                case (uint)EM_EVENT_IVS_TYPE.TAKENAWAYDETECTION:
                    {
                        dhs.dwEventType = EM_EVENT_IVS_TYPE.TAKENAWAYDETECTION;
                        NET_DEV_EVENT_TAKENAWAYDETECTION_INFO info = (NET_DEV_EVENT_TAKENAWAYDETECTION_INFO)Marshal.PtrToStructure(pEventInfo, typeof(NET_DEV_EVENT_TAKENAWAYDETECTION_INFO));
                        dhs.nChannelID = info.nChannelID;
                        dhs.szName = info.szName;
                        dhs.UTC = info.UTC.ToShortString();
                        dhs.nEventID = info.nEventID;
                        dhs.byImageIndex = info.byImageIndex;
                    }
                    break;
                // 徘徊事件
                case (uint)EM_EVENT_IVS_TYPE.WANDERDETECTION:
                    {
                        dhs.dwEventType = EM_EVENT_IVS_TYPE.WANDERDETECTION;
                        NET_DEV_EVENT_WANDER_INFO info = (NET_DEV_EVENT_WANDER_INFO)Marshal.PtrToStructure(pEventInfo, typeof(NET_DEV_EVENT_WANDER_INFO));
                        dhs.nChannelID = info.nChannelID;
                        dhs.szName = info.szName;
                        dhs.UTC = info.UTC.ToShortString();
                        dhs.nEventID = info.nEventID;
                        dhs.byImageIndex = info.byImageIndex;

                    }
                    break;
                // 翻越围栏事件
                case (uint)EM_EVENT_IVS_TYPE.CROSSFENCEDETECTION:
                    {
                        dhs.dwEventType = EM_EVENT_IVS_TYPE.CROSSFENCEDETECTION;
                        NET_DEV_EVENT_CROSSFENCEDETECTION_INFO info = (NET_DEV_EVENT_CROSSFENCEDETECTION_INFO)Marshal.PtrToStructure(pEventInfo, typeof(NET_DEV_EVENT_CROSSFENCEDETECTION_INFO));
                        dhs.nChannelID = info.nChannelID;
                        dhs.szName = info.szName;
                        dhs.UTC = info.UTC.ToShortString();
                        dhs.nEventID = info.nEventID;
                        dhs.byImageIndex = info.byImageIndex;
                    }
                    break;
                // 攀高检测事件
                case (uint)EM_EVENT_IVS_TYPE.TRAFFIC_RUNYELLOWLIGHT:
                    {
                        dhs.dwEventType = EM_EVENT_IVS_TYPE.TRAFFIC_RUNYELLOWLIGHT;
                        NET_DEV_EVENT_IVS_CLIMB_INFO info = (NET_DEV_EVENT_IVS_CLIMB_INFO)Marshal.PtrToStructure(pEventInfo, typeof(NET_DEV_EVENT_IVS_CLIMB_INFO));
                        dhs.nChannelID = info.nChannelID;
                        dhs.szName = info.szName;
                        dhs.UTC = info.UTC.ToShortString();
                        dhs.nEventID = info.nEventID;
                        dhs.byImageIndex = info.byImageIndex;

                    }
                    break;
                //人体特征事件
                case (uint)EM_EVENT_IVS_TYPE.HUMANTRAIT:
                    {
                        dhs.dwEventType = EM_EVENT_IVS_TYPE.HUMANTRAIT;
                        NET_DEV_EVENT_HUMANTRAIT_INFO info = (NET_DEV_EVENT_HUMANTRAIT_INFO)Marshal.PtrToStructure(pEventInfo, typeof(NET_DEV_EVENT_HUMANTRAIT_INFO));
                        dhs.nChannelID = info.nChannelID;
                        dhs.szName = info.szName;
                        dhs.UTC = info.UTC.ToShortString();
                        dhs.nEventID = info.nEventID;
                        List<string> imgs = new List<string>();
                        string pic_name = String.Format("./image/{0}_", info.nGroupID);
                        // 保存全景图片
                        if (info.stuSceneImage.nLength > 0)
                        {
                            byte[] bytes = new byte[info.stuSceneImage.nLength];
                            Marshal.Copy(new IntPtr(pBuffer.ToInt32() + info.stuSceneImage.nOffSet), bytes, 0, (int)info.stuSceneImage.nLength);
                            pic_name = pic_name + "全景图.jpg";
                            WriteFile(bytes, pic_name, (int)info.stuSceneImage.nLength);
                            imgs.Add(pic_name);
                        }
                        // 保存人脸图
                        if (info.stuFaceImage.nLength > 0)
                        {
                            pic_name = pic_name + "人脸图.jpg";
                            byte[] bytes = new byte[info.stuHumanImage.nLength];
                            Marshal.Copy(new IntPtr(pBuffer.ToInt32() + info.stuHumanImage.nOffSet), bytes, 0, (int)info.stuHumanImage.nLength);
                            WriteFile(bytes, pic_name, (int)info.stuHumanImage.nLength);
                            imgs.Add(pic_name);
                        }
                        // 保存人脸全景图
                        if (info.stuFaceSceneImage.nLength > 0)
                        {
                            pic_name = pic_name + "人脸全景图.jpg";
                            byte[] bytes = new byte[info.stuFaceSceneImage.nLength];
                            Marshal.Copy(new IntPtr(pBuffer.ToInt32() + info.stuFaceSceneImage.nOffSet), bytes, 0, (int)info.stuFaceSceneImage.nLength);
                            WriteFile(bytes, pic_name, (int)info.stuFaceSceneImage.nLength);
                            imgs.Add(pic_name);
                        }
                        //保存人体图
                        if (info.stuHumanImage.nLength > 0)
                        {
                            pic_name = pic_name + "人体图.jpg";
                            byte[] bytes = new byte[info.stuHumanImage.nLength];
                            Marshal.Copy(new IntPtr(pBuffer.ToInt32() + info.stuHumanImage.nOffSet), bytes, 0, (int)info.stuHumanImage.nLength);
                            WriteFile(bytes, pic_name, (int)info.stuHumanImage.nLength);
                            imgs.Add(pic_name);
                        }
                        dhs.szFilePath = imgs;
                    }
                    break;
                //交通路口事件
                case (uint)EM_EVENT_IVS_TYPE.TRAFFICJUNCTION:
                    {
                        dhs.dwEventType = EM_EVENT_IVS_TYPE.TRAFFICJUNCTION;
                        NET_DEV_EVENT_TRAFFICJUNCTION_INFO info = (NET_DEV_EVENT_TRAFFICJUNCTION_INFO)Marshal.PtrToStructure(pEventInfo, typeof(NET_DEV_EVENT_TRAFFICJUNCTION_INFO));
                        dhs.nChannelID = info.nChannelID;
                        dhs.szName = info.szName.ToString();
                        dhs.UTC = info.UTC.ToShortString();
                        dhs.nEventID = info.nEventID;
                        dhs.byImageIndex = info.byImageIndex;
                        List<string> imgs = new List<string>();
                        //车牌号、车牌颜色、车牌类型
                        string pic_name;
                        //车辆图
                        if (info.stuVehicle.bPicEnble != 0)
                        {
                            byte[] bytes = new byte[info.stuVehicle.stPicInfo.dwFileLenth];
                            Marshal.Copy(new IntPtr(pBuffer.ToInt32() + info.stuVehicle.stPicInfo.dwOffSet), bytes, 0, (int)info.stuVehicle.stPicInfo.dwFileLenth);
                            pic_name = String.Format("./image/{0}车辆图.jpg", info.stuFileInfo.nGroupId);
                            WriteFile(bytes, pic_name, (int)info.stuVehicle.stPicInfo.dwFileLenth);
                            imgs.Add(pic_name);
                        }
                        //车牌图
                        if (info.stuObject.bPicEnble != 0 && info.stuObject.stPicInfo.dwFileLenth != 0)
                        {
                            byte[] bytes = new byte[info.stuObject.stPicInfo.dwFileLenth];
                            Marshal.Copy(new IntPtr(pBuffer.ToInt32() + info.stuObject.stPicInfo.dwOffSet), bytes, 0, (int)info.stuObject.stPicInfo.dwFileLenth);
                            pic_name = String.Format("./image/{0}_{1}车牌图.jpg", info.stuFileInfo.nGroupId, info.stuFileInfo.bIndex);
                            WriteFile(bytes, pic_name, (int)info.stuObject.stPicInfo.dwFileLenth);
                            imgs.Add(pic_name);
                        }
                        //保存驾驶室人脸图，副驾驶室有人的话，有图片。
                        int i = 0;
                        for (; i < info.stCommInfo.nDriversNum; i++)
                        {
                            if (i == 0)
                            {
                                pic_name = String.Format("./image/{0}_{1}主驾驶室.jpg", info.stuFileInfo.nGroupId, i);
                            }
                            else
                            {
                                pic_name = String.Format("./image/{0}_{1}副驾驶室.jpg", info.stuFileInfo.nGroupId, i);
                            }
                            NET_MSG_OBJECT_EX[] obj = new NET_MSG_OBJECT_EX[info.stCommInfo.nDriversNum];
                            for (i = 0; i < info.stCommInfo.nDriversNum; ++i)
                            {
                                obj[i] = (NET_MSG_OBJECT_EX)Marshal.PtrToStructure(IntPtr.Add(info.stCommInfo.pstDriversInfo, i * Marshal.SizeOf(typeof(NET_MSG_OBJECT_EX))), typeof(NET_MSG_OBJECT_EX));
                                if (obj[i].bPicEnble != 0)
                                {
                                    byte[] bytes = new byte[obj[i].stPicInfo.dwFileLenth];
                                    Marshal.Copy(new IntPtr(pBuffer.ToInt32() + obj[i].stPicInfo.dwOffSet), bytes, 0, (int)obj[i].stPicInfo.dwFileLenth);
                                    WriteFile(bytes, pic_name, (int)obj[i].stPicInfo.dwFileLenth);
                                    imgs.Add(pic_name);
                                }
                            }
                        }
                        dhs.szFilePath = imgs;
                    }
                    break;
                default:
                    break;
            }
           // publisher.PublishAsync("DHServer", dhs);
            return 1;
        }
        internal static void WriteFile(byte[] bytes, string path, int length)
        {
            FileStream fs = new FileStream(path, FileMode.Create);
            fs.Write(bytes, 0, length);
            fs.Close();
        }
        private static void ReConnectCallBack(IntPtr lLoginID, IntPtr pchDVRIP, int nDVRPort, IntPtr dwUser)
        {

        }
        private static void DisConnectCallBack(IntPtr lLoginID, IntPtr pchDVRIP, int nDVRPort, IntPtr dwUser)
        {

        }
        //[HttpGet("TestPush")]
        //public ActionResult TestPush()
        //{
        //    DHServer dhs = new DHServer()
        //    {
        //        UTC = "2021-05-11",
        //        byImageIndex = 1,
        //        dwEventType = EM_EVENT_IVS_TYPE.ALARM_LOCALALARM,
        //        sSerialNumber = "dh12321312312"
        //    };
        //    _capPublisher.PublishAsync<DHServer>("DHServer", dhs);
        //    return Ok("添加成功");
        //}
    }
}
