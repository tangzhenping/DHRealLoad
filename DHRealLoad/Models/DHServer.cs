using NetSDKCS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DHRealLoad.Models
{
    /// <summary>
    /// 返回报警数据
    /// </summary>
    public class DHServer
    {
        /// <summary>
        /// 设备序列号
        /// </summary>
        public string sSerialNumber { get; set; }
        /// <summary>
        /// device type, refer to EM_NET_DEVICE_TYPE
        /// 设备类型,见枚举NET_DEVICE_TYPE
        /// </summary>
        public EM_NET_DEVICE_TYPE nDVRType { get; set; }
        /// <summary>
        ///  智能事件类型
        /// </summary>
        public EM_EVENT_IVS_TYPE dwEventType { get; set; }
        /// <summary>
        /// ChannelId
        /// 通道号
        /// </summary>
        public int nChannelID { get; set; }
        /// <summary>
        /// event name
        /// 事件名称
        /// </summary>
        public string szName { get; set; }
        /// <summary>
        /// 事件发生时间
        /// </summary>
        public string UTC { get; set; }
        /// <summary>
        /// event ID
        /// 事件ID
        /// </summary>
        public int nEventID { get; set; }
        /// <summary>
        /// Serial number of the picture, in the same time (accurate to seconds) may have multiple images, starting from 0
        /// 图片的序号, 同一时间内(精确到秒)可能有多张图片, 从0开始
        /// </summary>
        public byte byImageIndex { get; set; }
        /// <summary>
        /// 全景图片路径
        /// </summary>
        public List<string> szFilePath { get; set; }
    }
    /// <summary>
    /// 基本返回值
    /// </summary>
    public class Result
    {
        /// <summary>
        /// 构造
        /// </summary>
        public Result()
        {
            this.ErrorID = 0;
            this.Success = false;
            this.Message = "当前网络不畅通，请稍后再试.";
        }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 提示消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 错误ID
        /// </summary>
        public int ErrorID { get; set; }
    }
    /// <summary>
    /// 带实体的返回值
    /// </summary>
    public class Result1<T> : Result
    {
        /// <summary>
        /// 实体
        /// </summary>
        public T Model { get; set; }
    }
    /// <summary>
    /// 带列表的返回值
    /// </summary>
    public class Result2<T> : Result
    {
        /// <summary>
        /// 列表
        /// </summary>
        public List<T> Content { get; set; }
    }
    public class LoginParam
    {
        /// <summary>
        /// 设备IP
        /// </summary>
        public string pchDVRIP { get; set; }
        /// <summary>
        /// 设备端口
        /// </summary>
        public ushort wDVRPort { get; set; }
        /// <summary>
        /// 用户名
        /// </summary>
        public string pchUserName { get; set; }
        /// <summary>
        /// 密码
        /// </summary>
        public string pchPassword { get; set; }
    }
}
