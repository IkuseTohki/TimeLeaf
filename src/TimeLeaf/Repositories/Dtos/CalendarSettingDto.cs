using System;
using System.Collections.Generic;

namespace TimeLeaf.Repositories.Dtos
{
    public class CalendarSettingDto
    {
        public List<HolidayDto> Holidays { get; set; } = new List<HolidayDto>();
        public List<DayOfWeek> WorkdaysOfWeek { get; set; } = new List<DayOfWeek>();
    }

    public class HolidayDto
    {
        public DateTime Date { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
