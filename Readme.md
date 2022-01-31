# Kamanri
一个个人编写并使用的基于`.net Core 3.1`的类库.
包括:
- **一个轻量级对象关系映射(ORM)框架 -- Kamanri.Database**
- **一个事件驱动型网络套接字(WebSocket)框架 -- Kamanri.WebSockets**
- 一套对HTTP请求的封装 -- Kamanri.Http
- 一些工具类(如随机ID生成类，互斥锁类等等) -- Kamanri.Self

----

## 1. Kamanri.Database
### 1.1 简介
如前文所述，这是一个**轻量级对象关系映射(ORM)框架**.
众所周知，ORM框架是指能够将数据库中的表与面向对象语言中的对象互相绑定并提供一定数据库操作能力的一套解决方案.
本框架设计的出发点以及区别于其他的常见的ORM框架在于，它可以基于两类实体之间的关系查询(比较推荐这么做)，也可以直接基于SQL selection查询.本框架也保留使用者直接使用sql来操作数据库的能力.
本框架设计的核心为基于关系表中关系的查询.使用者可以在两个实体之间赋予任意形式的关系，并通过框架内置的方法来查询.
本框架基于`ASP .net Core`.
另外，本框架目前只适用于关系型数据库，并且需要项目自行提供到对应数据库的驱动包(如Mysql.Data).

### 1.2 使用方法

#### 1.2.1 准备

1. 在`ASP .net Core`项目中的`Startup.cs`引入框架的服务: 

Startup.cs
``` cs

using XXX;

namespace XXX
{
	public class Startup
	{
		
		public void ConfigureServices(IServiceCollection services)
		{
            ...
			
			services.AddKamanriDataBase(options =>
			{
				options.Server = Configuration["SQL:Server"];
				options.Port = Configuration["SQL:Port"];
				options.Database = Configuration["SQL:Database"];
				options.Uid = Configuration["SQL:Uid"];
				options.Pwd = Configuration["SQL:Pwd"];

			}, options => new MySql.Data.MySqlClient.MySqlConnection(options));

            ...

		}
	}
}

```
你可以直接将配置填写在`Startip.cs`中, 不过为了便于后续的部署与配置, 通常更建议这里采用的**json配置项载入**.
此时项目的`appsettings.json`应当配置为: 
``` json
{
  ...

  "SQL":{
	"Server": ${Server IP},
	"Port": ${Server Port},
	"Database": ${Server Database Name},
	"Uid": ${Database User Id},
	"Pwd": ${Database User Password}
  }
  ...
}
```

2. 自行为对应的数据库设置实体表和关系表.
设现有两种实体A和B, 需要建立这两种实体的实体表和他们的关系表.
注意对于实体A(表名为`ta`)和实体B(表名为`tb`)的关系表,其格式严格规定为如下: 

``` sql
create table ta_tb(
	ta bigint not null,
    tb bigint not null,
    relations varchar(255) not null,
	primary key (ta, tb),
    foreign key FK_TA (ta) references ta(ID),
    foreign key FK_TB (tb) references tb(ID)
);
```
对应的实体A和实体B需要有一个名为ID的自增主键:

``` sql
create table ta(
	ID bigint auto_increment primary key,
    p1 varchar(255)
);
create table tb(
	ID bigint auto_increment primary key,
    p2 varchar(255)
);
```
3. 在项目中新建Models文件夹, 并在其下新建`class A`和`class B`作为两种实体的对象模型: 

``` cs
...
    public class A
	{
		public string p1 { get; set; }
	}
...
    public class B
	{
		public int p2 { get; set; }
	}
...
```
- 注意这里一定要把实体中的属性**以属性的方式定义**(能通过Type.GetProperties()获取), 否则无法识别.

4. 让它们
- 继承 `Kamanri.Database.Models.Entity<TEntity>` 类.
- 重写`GetEntityFromDataReader`和`GetEntity`两个方法.
> - `GetEntityFromDataReader`方法用于从数据库返回的数据读取器`DbDataReader`中取出对应的实体.
> - `GetEntity`方法用于从本实体抽象类的类实例中获取实体, 统一返回this即可.
- 重写`TableName`属性, 赋值为对应的数据库表名.
- 为每个实体属性添加适当的自定义属性(如`ColumnName`为对应的数据库列名).
``` cs
using System.Data.Common;
using Kamanri.Database.Models;
using Kamanri.Database.Models.Attributes;
...
    public class A : Entity<A>
	{
        [ColumnName("p1")]
		public string P1 { get; set; }

        public override string TableName { get; set; } = "t1";

        public override A GetEntity() => this;

		public override A GetEntityFromDataReader(DbDataReader ddr) =>
			new A()
			{
				P1 = (string)ddr["p1"]
			};
	}
...
    public class B : Entity<B>
	{
		[ColumnName("p2")]
		public int P2 { get; set; }

        public override string TableName { get; set; } = "t2";

        public override B GetEntity() => this;

		public override B GetEntityFromDataReader(DbDataReader ddr) =>
			new B()
			{
				P2 = (string)ddr["p2"]
			};
	}
...
```

1. 在要使用实体框架的类中通过构造函数注入`DatabaseContext`类.

``` cs
public class Service1
{
    private readonly DatabaseContext _dbc;

    public UserService(DatabaseContext dbc, TagService tagService)
	{
		_dbc = dbc;
	}
}
```

#### 1.2.2 简单的操作

##### 简单的增删改查

- 插入A

``` cs
var a = new A()
{
    P1 = "XXX"
};

await _dbc.Insert<A>(a);
// 拿到数据库分配的ID以进行后续操作
var ID = await _dbc.SelectID<A>(a);

// 将已经分配到ID的实体插入
await _dbc.InsertWithID(a);
```

- 删除A
``` cs
await _dbc.Delete<A>(a);
```
- 修改A

``` cs
a.P1 = "XXX";
await _dbc.Update<A>(a);
```

- 查询A
``` cs
// 根据ID查询其他数据
await _dbc.Select<A>(a);
// 根据候选码查询ID
var ID = await _dbc.SelectID<A>(a);
// 查询所有
var allAList = await _dbc.SelectAll<A>();
// 使用SQL查询
var aList = await _dbc.SelectCustom<A>("P1 = 'XXX'");
```
他们也有对应的集合方法`Inserts`,`InsertsWithID`,`Deletes`,`Selects`和`SelectIDs`,此时输入对象变为了集合.

##### 结合实体关系的查询

例如ta_tb表中有如下行:
<table>
    <tr>
        <td>ta</td>
        <td>tb</td>
        <td>relations</td>
    </tr>
    <tr>
        <td>1</td>
        <td>2</td>
        <td>{"Type": ["R1"]}</td>
    </tr>
</table>

则表示ID为1的A实体和ID为2的B实体存在**键为Type, 值为R1**的关系.

可以通过A与对应的关系找到B: 

``` cs
var a = new A()
{
    ID = 1
};

var bList = _dbc.MappingSelect<A, B>(
    a,
    ID_IDList.OutPutType.Value,
    selection => selection.Type = new List<string>(){ "R1" }
);
```
上述语句可以将所有的`ta`列为1, `relations`中键为`Type`的值包括`R1`的所有tb对应的实体列出.
- `ID_IDList.OutPutType.Value` 表示输出实体的类型(B)在关系表中的位置(tb)为"值", 即第二位. 若为`ID_IDList.OutPutType.Key`则为第一位.

此外还有: 
> Mapping: 在数据库中匹配得到输入实体和对应的输出实体
> MappingUnionStatistics: 查找所有关系的并集并使用字典统计每一个输出实体与输入实体的所有关系
> MappingSelectUnionStatistics: 查找所有关系的并集并使用字典统计每一个输出实体与输入实体的所有关系,根据输入条件筛选符合条件的关系

----

## 2. Kamanri.WebSockets
### 2.1 简介
如前文所述，这是一个**事件驱动型网络套接字(WebSocket)框架**.
本框架基于`ASP .net Core`.
本框架实现树状结构的C/S拓扑，可作为客户端向一个服务端发出请求，也可以作为服务端接受多台来自客户端的请求, 一个服务程序可单独作为客户端或服务端使用，也可同时作为客户端或服务端使用.
本框架基于事件处理机制的思想，采用方法的表驱动方式，实现了一个类似于控制器的处理层，使得用户可以像编写控制器一样，为被路由到某个特定事件的消息编写相应的事件处理程序.
本框架目前已提供web端的js实现以及安卓端的kotlin实现.

### 2.2 使用方法

1. 添加`OnMessageMessage`自定义的事件处理类和`OnMessageMiddleware`自定义的事件处理类中间件类.

``` cs
    public class OnMessageService
    {

    }

    public class OnMessageMiddleware
	{
		public RequestDelegate _next;
		//启动OnMessageService服务
		public OnMessageService OnMessageService { get; set; }
		public OnMessageMiddleware(RequestDelegate next, OnMessageService onMsService)
		{
			_next = next;
			OnMessageService = onMsService;
		}

		public async Task Invoke(HttpContext httpContext)
		{
			await _next.Invoke(httpContext);
		}
	}
```


2. 在`ASP .net Core`项目中的`Startup.cs`引入框架的服务: 

Startup.cs
``` cs

using XXX;

namespace XXX
{
	public class Startup
	{
		
		public void ConfigureServices(IServiceCollection services)
		{
            ...
            services.AddControllers();
			// 添加服务
			services.AddKamanriWebSocket().AddSingleton<OnMessageService>();

            ...

		}

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseRouting();
            // 添加中间件
			app.UseKamanriWebSocket();

			app.UseMiddleware<OnMessageMiddleware>();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});
		}
	}
}

```

3. 如果该项目需要作为客户端, 则在`appsettings.json`添加: 

``` json
{
  ...

  "WebSocket" : {
	"URL" : ${Server URL}
  }

  ...
}
```
否则, 忽略这一步.

4. 如果该项目要作为服务端, 则创建一个控制器作为WebSocket的注入点: 

IndexController.cs
``` cs
    public class IndexController : ControllerBase
	{
		IWebSocketSession _wsSession;

		public IndexController(IWebSocketSession wsSession)
		{
			_wsSession = wsSession;
		}

		[HttpGet]
		[Route("/")]
		public async Task Indexer()
		{

			if (HttpContext.WebSockets.IsWebSocketRequest)
			{
				var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
				//注册WebSocket
				await _wsSession.AcceptWebSocketInjection(webSocket);
			}


		}


	}
```
否则, 忽略这一步.

5. 在`OnMessageService`中编写事件处理程序: 

``` cs
public class OnMessageService
{
    private readonly IWebSocketMessageService _wsmService;
    public OnMessageService(IWebSocketMessageService wsmService)
    {
        _wsmService = wsmService;
        _wsmService.AddEventHandler(WebSocketMessageEvent.OnConnect, OnConnect)
            .AddEventHandler(WebSocketMessageEvent.OnDisconnect, OnDisconnect);;
        ...
    }

    public Task<IList<WebSocketMessage>> OnConnect(IWebSocketSession session, IList<WebSocketMessage> messages)
    {
        ...
    }

    public Task<IList<WebSocketMessage>> OnDisconnect(IWebSocketSession session, IList<WebSocketMessage> messages)
    {
        ...
    }
    ...
}
```
- `IWebSocketMessageService.AddEventHandler`方法将对应的消息路由到对应的事件处理程序, 并将事件处理程序的返回值作为消息返回给发送消息方.
- 也可以利用`IWebSocketSession`单独发送消息.
----

## 3. Kamanri.Http
   ...
## 4. Kamanri.Self
   ...
