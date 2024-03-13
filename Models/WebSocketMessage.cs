using GameServer.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GameServer.Models
{
    /// <summary>
    /// 序列化的类
    /// </summary>
    public class WebSocketMessage
    {
        
        //[JsonIgnore] JSON中该属性将被忽略

        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("data")]
        public JToken Data { get; set; }

        [JsonProperty("error")]
        public ErrorEnum Error { get; set; }



        // 可以选择添加序列化和反序列化的帮助方法
        /// <summary>
        /// 序列化
        /// </summary>
        /// <returns></returns>
        public string Serialize() => JsonConvert.SerializeObject(this);
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static WebSocketMessage Deserialize(string json) => JsonConvert.DeserializeObject<WebSocketMessage>(json);

        //设置错误信息的默认值
        public WebSocketMessage()
        {
            Error = ErrorEnum.没有错误;
        }

    }

    
}
