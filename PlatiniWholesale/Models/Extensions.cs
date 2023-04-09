using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Data;
using System.Reflection;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Platini.Models
{
    public static class MyExtensions
    {
        public static List<T> CustomSort<T, TPropertyType>(this IEnumerable<T> collection, string propertyName, string sortOrder)
        {
            List<T> sortedlist = null;

            ParameterExpression pe = Expression.Parameter(typeof(T), "p");
            Expression<Func<T, TPropertyType>> expr = Expression.Lambda<Func<T, TPropertyType>>(Expression.Property(pe, propertyName), pe);

            if (!string.IsNullOrEmpty(sortOrder) && sortOrder == "desc")
                sortedlist = collection.OrderByDescending<T, TPropertyType>(expr.Compile()).ToList();
            else
                sortedlist = collection.OrderBy<T, TPropertyType>(expr.Compile()).ToList();

            return sortedlist;
        }
        
    }
   
}
