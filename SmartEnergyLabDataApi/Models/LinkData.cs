using System.Text.Json;

namespace SmartEnergyLabDataApi.Models;

public class LinkData<T>
    {
        public LinkData()
        {
            
        }
        public long DateTimeTicks { get; set; }
        public long TimeOutTicks { get; set; }
        public T Data { get; set; }

        public bool HasTimedOut()
        {
            var dateTime = new DateTime(DateTimeTicks);
            var timeSpan = new TimeSpan(TimeOutTicks);
            return ( DateTime.Now > dateTime + timeSpan);
        }

        public LinkData(T data, TimeSpan timeout)
        {
            TimeOutTicks = timeout.Ticks;
            DateTimeTicks = DateTime.Now.Ticks;
            Data = data;
        }

        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }

        public static LinkData<T> Deserialize(string input)
        {
            var newObj = JsonSerializer.Deserialize<LinkData<T>>(input);
            if (newObj.HasTimedOut())
            {
                return null;
            }
            else
            {
                return newObj;
            }
        }
    }