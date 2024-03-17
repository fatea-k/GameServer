任务目标:

	√ 基础通信协议(websocket)
	
	√ 用户登录
	
	√ 用户连接持久化
	
	√ 用户单一登录,确保一个账号只有一个连接在线
	
	通信优化
	
		√ 单条消息处理方法
		
			√ 单条消息对单用户即时发送(适用单人私有消息,如登录,客户端请求信息的返回,个人状态变更等)
			
			√ 单条消息对多用户并发推送(适用即时性要求较高的环境,比如战斗信息等,即时等级1)
			
			 √ 单条消息对多用户顺序推送(适用即时性要求一般的环境,比如场景内怪物刷新,移动,玩家的移动等等即时性要求一般的环境,即时等级2)
			
		多消条消息处理方法(适用聊天,区域信息推送)
		
			√ 多条消息的合并处理
			
				√ 消息存入缓冲池通道,对所有消息逐条合并
				
				√ 设置消息发送条件:合并的消息包大小(默认2k),计时器到期(5ms-100ms)
				
			多条消息的发送  (当触发任意条件,发送合并后的消息)
			
				并发发送(即时等级3)
				
				√ 逐包发送(即时等级4)
				
