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
本框架设计的出发点以及区别于其他的常见的ORM框架在于，它可以基于两类实体之间的关系查找(比较推荐这么做)，也可以直接基于SQL selection查找.本框架也保留使用者直接使用sql来操作数据库的能力.
本框架设计的核心为基于关系表中关系的查找.使用者可以在两个实体之间赋予任意形式的关系，并通过框架内置的方法来查找.
本框架基于asp .net core.
另外，本框架目前只适用于关系型数据库，并且需要项目自行提供到对应数据库的驱动包(如Mysql.Data).

### 1.2 使用方法


----

## 2. Kamanri.WebSockets
### 2.1 简介
如前文所述，这是一个**事件驱动型网络套接字(WebSocket)框架**.
本框架基于asp .net core.
本框架实现树状结构的C/S拓扑，可作为客户端向一个服务端发出请求，也可以作为服务端接受多台来自客户端的请求, 一个服务程序可单独作为客户端或服务端使用，也可同时作为客户端或服务端使用.
本框架基于事件处理机制的思想，采用方法的表驱动方式，实现了一个类似于控制器的处理层，使得用户可以像编写控制器一样，为被路由到某个特定事件的消息编写相应的事件处理程序.
本框架目前已提供web端的js实现以及安卓端的kotlin实现.

----

## 3. Kamanri.Http
   ...
## 4. Kamanri.Self
   ...
