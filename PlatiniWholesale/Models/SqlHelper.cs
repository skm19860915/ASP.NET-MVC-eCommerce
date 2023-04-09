using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Threading.Tasks;
using System.Reflection;

namespace Platini.Models
{
    public static class SqlHelper
    {
        static string CONNECTION_STRING = ConfigurationManager.ConnectionStrings["DbConnection"].ConnectionString;

        internal static DataTable ExecuteTable(string CommandName, CommandType cmdType)
        {
            DataTable table = null;
            using (SqlConnection con = new SqlConnection(CONNECTION_STRING))
            {
                using (SqlCommand cmd = con.CreateCommand())
                {
                    cmd.CommandType = cmdType;
                    cmd.CommandText = CommandName;
                    try
                    {
                        if (con.State != ConnectionState.Open)
                        {
                            con.Open();
                        }
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            table = new DataTable();
                            da.Fill(table);
                        }
                    }
                    catch
                    {
                        throw;
                    }
                }
            }

            return table;
        }

        internal static DataTable ExecuteTable(string CommandName, CommandType cmdType, SqlParameter[] param)
        {
            DataTable table = new DataTable();
            using (SqlConnection con = new SqlConnection(CONNECTION_STRING))
            {
                using (SqlCommand cmd = con.CreateCommand())
                {
                    cmd.CommandType = cmdType;
                    cmd.CommandText = CommandName;
                    cmd.Parameters.AddRange(param);
                    try
                    {
                        if (con.State != ConnectionState.Open)
                        {
                            con.Open();
                        }
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(table);
                        }
                    }
                    catch
                    {
                        throw;
                    }
                }
            }

            return table;
        }

        //internal static async Task<DataTable> ExecuteTableAsync(string CommandName, CommandType cmdType)
        //{
        //    DataTable table = new DataTable();
        //    using (SqlConnection con = new SqlConnection(CONNECTION_STRING))
        //    {
        //        using (SqlCommand cmd = con.CreateCommand())
        //        {
        //            cmd.CommandType = cmdType;
        //            cmd.CommandText = CommandName;
        //            try
        //            {
        //                if (con.State != ConnectionState.Open)
        //                {
        //                    await con.OpenAsync();
        //                }
        //                SqlDataReader sdr = await cmd.ExecuteReaderAsync();                       
        //                table.Load(sdr);
        //            }
        //            catch
        //            {
        //                throw;
        //            }
        //        }
        //    }
        //    return table;
        //}

        //internal static async Task<DataTable> ExecuteTableAsync(string CommandName, CommandType cmdType, SqlParameter[] param)
        //{
        //    DataTable table = new DataTable();
        //    using (SqlConnection con = new SqlConnection(CONNECTION_STRING))
        //    {
        //        using (SqlCommand cmd = con.CreateCommand())
        //        {
        //            cmd.CommandType = cmdType;
        //            cmd.CommandText = CommandName;
        //            cmd.Parameters.AddRange(param);
        //            try
        //            {
        //                if (con.State != ConnectionState.Open)
        //                {
        //                    await con.OpenAsync();
        //                }
        //                SqlDataReader sdr = await cmd.ExecuteReaderAsync();
        //                table.Load(sdr);
        //            }
        //            catch
        //            {
        //                throw;
        //            }
        //        }
        //    }
        //    return table;
        //}

        internal static List<T> ExecuteList<T>(string CommandName, CommandType cmdType) where T : class, new()
        {
            List<T> list = null;
            using (SqlConnection con = new SqlConnection(CONNECTION_STRING))
            {
                using (SqlCommand cmd = con.CreateCommand())
                {
                    cmd.CommandType = cmdType;
                    cmd.CommandText = CommandName;
                    try
                    {
                        if (con.State != ConnectionState.Open)
                        {
                            con.Open();
                        }
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable table = new DataTable();
                            da.Fill(table);
                            list = new List<T>();
                            foreach (var row in table.AsEnumerable())
                            {
                                T obj = new T();
                                foreach (var prop in obj.GetType().GetProperties())
                                {
                                    try
                                    {
                                        PropertyInfo propertyInfo = obj.GetType().GetProperty(prop.Name);
                                        propertyInfo.SetValue(obj, Convert.ChangeType(row[prop.Name], propertyInfo.PropertyType), null);
                                    }
                                    catch
                                    {
                                        continue;
                                    }
                                }
                                list.Add(obj);
                            }
                        }
                    }
                    catch
                    {
                        throw;
                    }
                }
            }
            return list;
        }

        internal static List<T> ExecuteList<T>(string CommandName, CommandType cmdType, SqlParameter[] param) where T : class, new()
        {
            List<T> list = null;
            using (SqlConnection con = new SqlConnection(CONNECTION_STRING))
            {
                using (SqlCommand cmd = con.CreateCommand())
                {
                    cmd.CommandType = cmdType;
                    cmd.CommandText = CommandName;
                    cmd.Parameters.AddRange(param);
                    try
                    {
                        if (con.State != ConnectionState.Open)
                        {
                            con.Open();
                        }
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable table = new DataTable();
                            da.Fill(table);
                            list = new List<T>();
                            foreach (var row in table.AsEnumerable())
                            {
                                T obj = new T();
                                foreach (var prop in obj.GetType().GetProperties())
                                {
                                    try
                                    {
                                        PropertyInfo propertyInfo = obj.GetType().GetProperty(prop.Name);
                                        propertyInfo.SetValue(obj, Convert.ChangeType(row[prop.Name], propertyInfo.PropertyType), null);
                                    }
                                    catch
                                    {
                                        continue;
                                    }
                                }
                                list.Add(obj);
                            }
                        }
                    }
                    catch
                    {
                        throw;
                    }
                }
            }
            return list;
        }

        //internal static async Task<List<T>> ExecuteListAsync<T>(string CommandName, CommandType cmdType) where T : class, new()
        //{
        //    List<T> list = null;
        //    using (SqlConnection con = new SqlConnection(CONNECTION_STRING))
        //    {
        //        using (SqlCommand cmd = con.CreateCommand())
        //        {
        //            cmd.CommandType = cmdType;
        //            cmd.CommandText = CommandName;
        //            try
        //            {
        //                if (con.State != ConnectionState.Open)
        //                {
        //                    await con.OpenAsync();
        //                }
        //                DataTable table = new DataTable();
        //                SqlDataReader sdr = await cmd.ExecuteReaderAsync();
        //                table.Load(sdr);
        //                list = new List<T>();
        //                foreach (var row in table.AsEnumerable())
        //                {
        //                    T obj = new T();
        //                    foreach (var prop in obj.GetType().GetProperties())
        //                    {
        //                        try
        //                        {
        //                            PropertyInfo propertyInfo = obj.GetType().GetProperty(prop.Name);
        //                            propertyInfo.SetValue(obj, Convert.ChangeType(row[prop.Name], propertyInfo.PropertyType), null);
        //                        }
        //                        catch
        //                        {
        //                            continue;
        //                        }
        //                    }
        //                    list.Add(obj);
        //                }
        //            }
        //            catch
        //            {
        //                throw;
        //            }
        //        }
        //    }
        //    return list;
        //}

        //internal static async Task<List<T>> ExecuteListAsync<T>(string CommandName, CommandType cmdType, SqlParameter[] param) where T : class, new()
        //{
        //    List<T> list = null;
        //    using (SqlConnection con = new SqlConnection(CONNECTION_STRING))
        //    {
        //        using (SqlCommand cmd = con.CreateCommand())
        //        {
        //            cmd.CommandType = cmdType;
        //            cmd.CommandText = CommandName;
        //            cmd.Parameters.AddRange(param);
        //            try
        //            {
        //                if (con.State != ConnectionState.Open)
        //                {
        //                    await con.OpenAsync();
        //                }
        //                SqlDataReader sdr = await cmd.ExecuteReaderAsync();
        //                DataTable table = new DataTable();
        //                table.Load(sdr);
        //                list = new List<T>();
        //                foreach (var row in table.AsEnumerable())
        //                {
        //                    T obj = new T();
        //                    foreach (var prop in obj.GetType().GetProperties())
        //                    {
        //                        try
        //                        {
        //                            PropertyInfo propertyInfo = obj.GetType().GetProperty(prop.Name);
        //                            propertyInfo.SetValue(obj, Convert.ChangeType(row[prop.Name], propertyInfo.PropertyType), null);
        //                        }
        //                        catch
        //                        {
        //                            continue;
        //                        }
        //                    }
        //                    list.Add(obj);
        //                }
        //            }
        //            catch
        //            {
        //                throw;
        //            }
        //        }
        //    }
        //    return list;
        //}

        internal static bool ExecuteNonQuery(string CommandName, CommandType cmdType)
        {
            int result = 0;

            using (SqlConnection con = new SqlConnection(CONNECTION_STRING))
            {
                using (SqlCommand cmd = con.CreateCommand())
                {
                    cmd.CommandType = cmdType;
                    cmd.CommandText = CommandName;

                    try
                    {
                        if (con.State != ConnectionState.Open)
                        {
                            con.Open();
                        }

                        result = cmd.ExecuteNonQuery();
                    }
                    catch
                    {
                        throw;
                    }
                }
            }

            return (result > 0);
        }

        internal static bool ExecuteNonQuery(string CommandName, CommandType cmdType, SqlParameter[] pars)
        {
            int result = 0;
            using (SqlConnection con = new SqlConnection(CONNECTION_STRING))
            {
                using (SqlCommand cmd = con.CreateCommand())
                {
                    cmd.CommandType = cmdType;
                    cmd.CommandText = CommandName;
                    cmd.Parameters.AddRange(pars);
                    cmd.CommandTimeout = cmd.Connection.ConnectionTimeout;
                    try
                    {
                        if (con.State != ConnectionState.Open)
                        {
                            con.Open();
                        }

                        result = cmd.ExecuteNonQuery();
                    }
                    catch
                    {
                        throw;
                    }
                }
            }

            return (result > 0);
        }

        //internal static async Task<bool> ExecuteNonQueryAsync(string CommandName, CommandType cmdType)
        //{
        //    int result = 0;
        //    using (SqlConnection con = new SqlConnection(CONNECTION_STRING))
        //    {
        //        using (SqlCommand cmd = con.CreateCommand())
        //        {
        //            cmd.CommandType = cmdType;
        //            cmd.CommandText = CommandName;
        //            try
        //            {
        //                if (con.State != ConnectionState.Open)
        //                {
        //                    await con.OpenAsync();
        //                }
        //                result = await cmd.ExecuteNonQueryAsync();
        //            }
        //            catch
        //            {
        //                throw;
        //            }
        //        }
        //    }
        //    return (result > 0);
        //}

        //internal static async Task<bool> ExecuteNonQueryAsync(string CommandName, CommandType cmdType, SqlParameter[] pars)
        //{
        //    int result = 0;
        //    using (SqlConnection con = new SqlConnection(CONNECTION_STRING))
        //    {
        //        using (SqlCommand cmd = con.CreateCommand())
        //        {
        //            cmd.CommandType = cmdType;
        //            cmd.CommandText = CommandName;
        //            cmd.Parameters.AddRange(pars);
        //            cmd.CommandTimeout = cmd.Connection.ConnectionTimeout;
        //            try
        //            {
        //                if (con.State != ConnectionState.Open)
        //                {
        //                    await con.OpenAsync();
        //                }
        //                result = await cmd.ExecuteNonQueryAsync();
        //            }
        //            catch
        //            {
        //                throw;
        //            }
        //        }
        //    }
        //    return (result > 0);
        //}

        internal static int ExecuteScaler(string CommandName, CommandType cmdType)
        {
            int result = 0;

            using (SqlConnection con = new SqlConnection(CONNECTION_STRING))
            {
                using (SqlCommand cmd = con.CreateCommand())
                {
                    cmd.CommandType = cmdType;
                    cmd.CommandText = CommandName;
                    try
                    {
                        if (con.State != ConnectionState.Open)
                        {
                            con.Open();
                        }
                        result = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    catch
                    {
                        throw;
                    }
                }
            }
            return result;
        }

        internal static int ExecuteScaler(string CommandName, CommandType cmdType, SqlParameter[] pars)
        {
            int result = 0;
            using (SqlConnection con = new SqlConnection(CONNECTION_STRING))
            {
                using (SqlCommand cmd = con.CreateCommand())
                {
                    cmd.CommandType = cmdType;
                    cmd.CommandText = CommandName;
                    cmd.Parameters.AddRange(pars);
                    try
                    {
                        if (con.State != ConnectionState.Open)
                        {
                            con.Open();
                        }
                        result = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    catch
                    {
                        throw;
                    }
                }
            }
            return result;
        }

        //internal static async Task<int> ExecuteScalerAsync(string CommandName, CommandType cmdType)
        //{
        //    int result = 0;
        //    using (SqlConnection con = new SqlConnection(CONNECTION_STRING))
        //    {
        //        using (SqlCommand cmd = con.CreateCommand())
        //        {
        //            cmd.CommandType = cmdType;
        //            cmd.CommandText = CommandName;
        //            try
        //            {
        //                if (con.State != ConnectionState.Open)
        //                {
        //                    await con.OpenAsync();
        //                }
        //                result = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        //            }
        //            catch
        //            {
        //                throw;
        //            }
        //        }
        //    }
        //    return result;
        //}

        //internal static async Task<int> ExecuteScalerAsync(string CommandName, CommandType cmdType, SqlParameter[] pars)
        //{
        //    int result = 0;
        //    using (SqlConnection con = new SqlConnection(CONNECTION_STRING))
        //    {
        //        using (SqlCommand cmd = con.CreateCommand())
        //        {
        //            cmd.CommandType = cmdType;
        //            cmd.CommandText = CommandName;
        //            cmd.Parameters.AddRange(pars);
        //            try
        //            {
        //                if (con.State != ConnectionState.Open)
        //                {
        //                    await con.OpenAsync();
        //                }
        //                result = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        //            }
        //            catch
        //            {
        //                throw;
        //            }
        //        }
        //    }
        //    return result;
        //}

        internal static List<T> DataTableToList<T>(this DataTable table) where T : class, new()
        {
            try
            {
                List<T> list = new List<T>();
                foreach (var row in table.AsEnumerable())
                {
                    T obj = new T();
                    foreach (var prop in obj.GetType().GetProperties())
                    {
                        try
                        {
                            PropertyInfo propertyInfo = obj.GetType().GetProperty(prop.Name);
                            propertyInfo.SetValue(obj, Convert.ChangeType(row[prop.Name], propertyInfo.PropertyType), null);
                        }
                        catch
                        {
                            continue;
                        }
                    }
                    list.Add(obj);
                }
                return list;
            }
            catch
            {
                return null;
            }
        }

    }
}
