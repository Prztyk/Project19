# Project Glossary

This document serves as a reference point for terms, acronyms, and concepts used throughout the project.

## Table of Contents
- [A](#a)
- [D](#d)
- [I](#i)
- [P](#p)
- [R](#r)

---

## D

### DTO (Data Transfer Object)
An object that carries data between processes in order to reduce the number of method calls. It contains no business logic.

*Example:*
```csharp
public record UserDto(Guid Id, string Email);
```

## I

### Immutability
A design principle where an object's state cannot be modified after it has been created. In C#, immutability helps prevent side effects, ensures thread safety, and makes code easier to reason about. It is commonly implemented using `readonly` fields, `{ get; }` only properties, `init` only setters, or C# 9 `records`.

## P

### POCO (Plain Old CLR Object)
A simple class that does not depend on any framework-specific base class or external library components. It focuses entirely on data and business logic.

*Example:*
```csharp
public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}
```

## R

### Record (C# 9.0)
A lightweight, immutable data structure introduced in C# 9 that provides built-in functionality for data encapsulation. Unlike standard classes, records use value-based equality (two different instances are considered equal if all their fields have the same values) and offer a concise syntax for declaration.

```csharp
// A concise positional record declaration
public record User(string Name, string Email);

// Value-based equality in action
var user1 = new User("Alice", "alice@example.com");
var user2 = new User("Alice", "alice@example.com");

bool areEqual = user1 == user2; // True (even though they are different instances)
```