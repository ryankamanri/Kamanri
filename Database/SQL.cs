using System;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.Common;
using Microsoft.Extensions.Logging;
using Kamanri.Utils;

namespace Kamanri.Database
{
	/// <summary>
	/// 数据库访问类
	/// </summary>
	public sealed class SQL
	{

		public class SqlConnectOptions
		{
			public string Server { get; set; }
			public string Port { get; set; }
			public string Database { get; set; }
			public string Uid { get; set; }
			public string Pwd { get; set; }

			public int NumberOfConnections { get; set; } = 3;

			public override string ToString()
			{
				return $"Server={Server};Port={Port};Database={Database};Uid={Uid};Pwd={Pwd};";
			}
		}

		private readonly ILogger _logger;

		private readonly SqlConnectOptions options;

		private Pool<DbConnection> connectionPool;

		private const string defaultExpression = "select * from tags where ID = -1";

		/// <summary>
		/// 对数据库访问服务的初始化
		/// </summary>
		/// <param name="SetOptions">一个匿名函数,用于设置数据库连接字符串</param>
		public SQL(Action<SqlConnectOptions> SetOptions, Func<string,DbConnection> SetConnection, ILoggerFactory loggerFactory)
		{
			_logger = loggerFactory.CreateLogger<SQL>();
			options = new SqlConnectOptions();
			SetOptions(options);
			try
			{
				connectionPool = new Pool<DbConnection>(3, () => 
				{
					var connection = SetConnection(options.ToString());
					connection.Open();
					return connection;
				});
				KeepSession();
			}
			catch (Exception e)
			{
				throw new DataBaseException($"Falied To Open The Connection To Database. Connect Options : {options}", e);
			}
		}

		private void KeepSession()
		{
			new Task(() =>
			{
				while (true)
				{
					connectionPool.Map(async (connection, mutex) =>
					{

						mutex.Wait();
						using DbCommand command = connection.CreateCommand();
						try
						{
							command.CommandText = defaultExpression;
							(await command.ExecuteReaderAsync()).Close();
							mutex.Signal();
						}
						catch (Exception e)
						{
							mutex.Signal();
							throw new DataBaseException($"Failed To Send The Keep Session Sign \n", e);
						}

					});
					System.Threading.Thread.Sleep(4 * 60 * 60 * 1000);
				}
			}).Start();

		}



		/// <summary>
		/// 执行查询语句
		/// 返回值DbDataReader可以经过模型中对应的实体的GetList方法处理成一个IList,
		/// </summary>
		/// <param name="expression">SQL表达式</param>
		/// <returns></returns>
		public async Task<KeyValuePair<DbDataReader, Mutex>> Query(string expression)
		{
			
			var connectionAndMutex = connectionPool.AllocateAndAssignMutex();
			connectionAndMutex.Value.Wait();
			using DbCommand command = connectionAndMutex.Key.CreateCommand();
			try
			{
				command.CommandText = expression;
				_logger.LogDebug($"[{DateTime.Now}] : Execute The SQL Query Expression : '{expression}'");
				return new KeyValuePair<DbDataReader, Mutex>(await command.ExecuteReaderAsync(), connectionAndMutex.Value);
			}
			catch (Exception e)
			{
				connectionAndMutex.Value.Signal();
				throw new DataBaseException($"Failed To Execute The SQL Query Expression : '{expression}'", e);
			}



		}

		public async Task<KeyValuePair<DbDataReader,Mutex>> Query(string expression, Func<DbCommand, ICollection<DbParameter>> SetParameter)
		{
			var connectionAndMutex = connectionPool.AllocateAndAssignMutex();
			connectionAndMutex.Value.Wait();
			using DbCommand command = connectionAndMutex.Key.CreateCommand();
			try
			{
				command.CommandText = expression;
				var parameters = SetParameter(command);
				if (parameters != null)
				{
					foreach (var param in parameters)
					{
						command.Parameters.Add(param);
					}
				}

				_logger.LogDebug($"[{DateTime.Now}] : Execute The SQL Query Expression : '{expression}', Set Parameters");
				return new KeyValuePair<DbDataReader, Mutex>(await command.ExecuteReaderAsync(), connectionAndMutex.Value);
			}
			catch (Exception e)
			{
				connectionAndMutex.Value.Signal();
				throw new DataBaseException($"Failed To Execute The SQL Query Expression : '{expression}'", e);
			}

		}

		/// <summary>
		///  执行非查询语句
		/// </summary>
		/// <param name="expression">SQL表达式</param>
		public async Task Execute(string expression)
		{
			var connectionAndMutex = connectionPool.AllocateAndAssignMutex();
			try
			{
				connectionAndMutex.Value.Wait();
				using (DbCommand command = connectionAndMutex.Key.CreateCommand())
				{
					command.CommandText = expression;
					_logger.LogDebug($"[{DateTime.Now}] : Execute The SQL NonQuery Expression : '{expression}'");
					await command.ExecuteNonQueryAsync();
				}
				connectionAndMutex.Value.Signal();
			}
			catch (Exception e)
			{
				connectionAndMutex.Value.Signal();
				throw new DataBaseException($"Failed To Execute The SQL NonQuery Expression : '{expression}'", e);
			}
		}

		public async Task Execute(string expression, Func<DbCommand, ICollection<DbParameter>> SetParameter)
		{
			var connectionAndMutex = connectionPool.AllocateAndAssignMutex();
			try
			{
				connectionAndMutex.Value.Wait();
				using (DbCommand command = connectionAndMutex.Key.CreateCommand())
				{
					command.CommandText = expression;
					var parameters = SetParameter(command);
					if (parameters != null)
					{
						foreach (var param in parameters)
						{
							command.Parameters.Add(param);
						}
					}
					_logger.LogDebug($"[{DateTime.Now}] : Execute The SQL NonQuery Expression : '{expression}', Set Parameters");
					await command.ExecuteNonQueryAsync();
				}
				connectionAndMutex.Value.Signal();
			}
			catch (Exception e)
			{
				connectionAndMutex.Value.Signal();
				throw new DataBaseException($"Failed To Execute The SQL NonQuery Expression : '{expression}'", e);
			}
		}
	}
}
