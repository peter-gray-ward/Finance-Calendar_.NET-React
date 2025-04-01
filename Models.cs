using Npgsql;
using System.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
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
        public double CheckingBalance { get; set; } = 0.0;
        [Required]
        public string Password { get; set; } = string.Empty;
        public string ProfileThumbnailBase64 { get; set; } = string.Empty;
        public string TimeZone { get; set; } = "Eastern Standard Time";
        public virtual Account Account { get; set; } = new Account();
    }

    public class Event
    {
        [Key]
        [Required]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid RecurrenceId { get; set; } = Guid.NewGuid();
        public string Summary { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public DateTime RecurrenceEndDate { get; set; } = DateTime.UtcNow;
        public double Amount { get; set; } = 0.0;
        public double Total { get; set; } = 0.0;
        public double Balance { get; set; } = 0.0;
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
        public double Amount { get; set; } = 0.0;
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime RecurrenceEndDate { get; set; } = DateTime.UtcNow;
        public string Frequency { get; set; } = "monthly";

        [ForeignKey("User")]
        public Guid UserId { get; set; } = Guid.NewGuid();
    }

    public class Debt
    {
        [Key]
        [Required]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public double Balance { get; set; } = 0.0;
        public double Interest { get; set; } = 0.0;
        public string Link { get; set; } = string.Empty;

        [ForeignKey("User")]
        public Guid UserId { get; set; } = Guid.NewGuid();
    }

    public class Account
    {
        [Key]
        public Guid UserId { get; set; }
        public List<Expense> Expenses { get; set; } = new List<Expense>();
        public List<Debt> Debts { get; set; } = new List<Debt>();
        public List<Event> Calendar { get; set; } = new List<Event>();
        public int Month { get; set; } = DateTime.UtcNow.Month;
        public int Year { get; set; } = DateTime.UtcNow.Year;
    }

    public class Authentication
    {
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public User User { get; set; } = new User();
    }

    public class Day
    {
        public int Date { get; set; } = DateTime.UtcNow.Day;
        public string Name { get; set; } = string.Empty;
        public List<Event> Events { get; set; } = new List<Event>();
        public int Year { get; set; } = DateTime.UtcNow.Year;
        public int Month { get; set; } = DateTime.UtcNow.Month;
        public bool IsTodayOrLater { get; set; } = false;
        public bool IsToday { get; set; } = false;
        public double Total { get; set; } = 0.0;
        public int DOW { get; set; } = 0;
    }

    public record ServiceResponse<T> 
    {
        public bool Success { get; set; } = false;
        public string? Message { get; set; }
        public User? User { get; set; }
        public T? Data { get; set; }
        public ServiceResponse(bool success, string? message, User? user, T? data)
        {
            this.Success = success;
            this.Message = message;
            this.User = user;
            this.Data = data;
        }
        public class Builder
        {
            private bool _success;
            private string? _message;
            private User? _user;
            private T? _data;
            public Builder success(bool success)
            {
                this._success = success;
                return this;
            }
            public Builder message(string message)
            {
                this._message = message;
                return this;
            }
            public Builder user(User user)
            {
                this._user = user;
                return this;
            }
            public Builder data(T data)
            {
                this._data = data;
                return this;
            }
            public ServiceResponse<T> build()
            {
                return new ServiceResponse<T>(_success, _message, _user, _data);
            }
        }
    }


    public record ApiResponse<T>
    {
        public bool Success { get; set; } = false;
        public string? Message { get; init; } = "";
        public T? Data { get; init; }
        public User? User { get; init; }

        public ApiResponse(bool success, string? message, T? data, User? user)
        {
            Success = success;
            Message = message;
            Data = data;
            User = user;
        }

        public class Builder
        {
            private string? _message;
            private bool _success = false;
            private T? _data;
            private User? _user;
            public Builder message(string message)
            {
                this._message = message;
                return this;
            }
            public Builder success(bool success)
            {
                this._success = success;
                return this;
            }
            public Builder data(T data)
            {
                this._data = data;
                return this;
            }
            public Builder user(User user)
            {
                this._user = user;
                return this;
            }
            public ApiResponse<T> build()
            {
                return new ApiResponse<T>(_success, _message, _data, _user);
            }
        }
    }
}