using Microsoft.Extensions.AI;
using Showcase.Shared.AIExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AI.Voice.Tools;

public class ReferenceDataTools : IAIToolHandler
{
    public IEnumerable<AIFunction> GetAITools()
    {
        var tools = GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<AIToolAttribute>() is not null)
            .Select(tool =>
            {
                var attribute = tool.GetCustomAttribute<AIToolAttribute>();

                return AIFunctionFactory.Create(tool, this, options: new()
                {
                    Name = attribute?.Name,
                    Description = attribute?.Description
                });
            });
        foreach (var tool in tools)
        {
            yield return tool;
        }
    }

    [AITool(name: "weather", description: "Gets the weather")]
    public static string GetWeather() => Random.Shared.NextDouble() > 0.5 ? "It's sunny" : "It's raining";

    [AITool(name: "Room Capacity", description: "Returns the number of people that can fit in a room.")]
    public static int GetRoomCapacity(RoomType roomType)
    {
        return roomType switch
        {
            RoomType.ShuttleSimulator => throw new InvalidOperationException("No longer available"),
            RoomType.NorthAtlantisLawn => 450,
            RoomType.VehicleAssemblyBuilding => 12000,
            _ => throw new NotSupportedException($"Unknown room type: {roomType}"),
        };
    }

    public enum RoomType
    {
        ShuttleSimulator,
        NorthAtlantisLawn,
        VehicleAssemblyBuilding,
    }
}
