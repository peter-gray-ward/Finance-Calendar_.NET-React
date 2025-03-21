using Npgsql;
using System.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace FinanceCalendar
{
    public class User
    {
        [Key]
        [Required]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public string UserName { get; set; } = string.Empty;
        [Required]
        public decimal CheckingBalance { get; set; } = 0.0m;
        [Required]
        public string Password { get; set; } = string.Empty;
        public string ProfileThumbnailBase64 { get; set; } = string.Empty;
        public virtual Account Account { get; set; } = new Account();
    }

    public class Event
    {
        [Key]
        [Required]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid RecurrenceId { get; set; } = Guid.NewGuid();
        public string Summary { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Now;
        public DateTime RecurrenceEndDate { get; set; } = DateTime.Now;
        public decimal Amount { get; set; } = 0.0m;
        public decimal Total { get; set; } = 0.0m;
        public decimal Balance { get; set; } = 0.0m;
        public bool Exclude { get; set; } = false;
        public string Frequency { get; set; } = string.Empty;

        [ForeignKey("User")]
        public Guid UserId { get; set; } = Guid.NewGuid();
    }

    public class Expense
    {
        [Key]
        [Required]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; } = 0.0m;
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime RecurrenceEndDate { get; set; } = DateTime.Now;
        public string Frequency { get; set; } = string.Empty;

        [ForeignKey("User")]
        public Guid UserId { get; set; } = Guid.NewGuid();
    }

    public class Account
    {
        [Key]
        public Guid UserId { get; set; }
        public List<Expense> Expenses { get; set; } = new List<Expense>();
        public int Month { get; set; } = DateTime.Now.Month;
        public int Year { get; set; } = DateTime.Now.Year;
    }

    public class Authentication
    {
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public User User { get; set; } = new User();
    }

    public class Day
    {
        public int Date { get; set; } = DateTime.Now.Day;
        public string Name { get; set; } = string.Empty;
        public List<Event> Events { get; set; } = new List<Event>();
        public int Year { get; set; } = DateTime.Now.Year;
        public int Month { get; set; } = DateTime.Now.Month;
        public bool IsTodayOrLater { get; set; } = false;
        public bool IsToday { get; set; } = false;
        public decimal Total { get; set; } = 0.0m;
    }

    public record ApiResponse<T>
    {
        public string Message { get; init; }
        public string Error { get; init; }
        public T Data { get; init; }

        public ApiResponse(string message, string error, T data)
        {
            Message = message;
            Error = error;
            Data = data;    
        }
    }
}