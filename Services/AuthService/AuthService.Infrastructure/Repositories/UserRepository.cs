using System;
using System.Collections.Generic;
using System.Text;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _db;
    public UserRepository(AuthDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(Guid id) => _db.Users.FindAsync(id).AsTask();
    public Task<User?> GetByEmailAsync(string e) => _db.Users.FirstOrDefaultAsync(u => u.Email == e);
    public Task<bool> EmailExistsAsync(string e) => _db.Users.AnyAsync(u => u.Email == e);
    public async Task AddAsync(User user) => await _db.Users.AddAsync(user);
    public Task UpdateAsync(User user) { _db.Users.Update(user); return Task.CompletedTask; }
    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
