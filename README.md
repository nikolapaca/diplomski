# Pivot

Web aplikacija za organizaciju zadataka i timski rad, urađena kao **diplomski rad**.

## O projektu

Pivot omogućava korisnicima da zadatke organizuju na tablama (boards). Svaka tabla sadrži liste, a lista kartice
koje se prevlače između lista. Tablu je moguće podeliti sa saradnicima, kartice dodeljivati članovima tima,
komentarisati ih i prilagati slike. Svaka izmena se beleži u istoriji aktivnosti, a korisnici u realnom vremenu
dobijaju obaveštenja o događajima koji ih se tiču.

Projekat se sastoji iz tri dela koji se pokreću zajedno preko Docker Compose-a:
- `Back-end/Pivot` – REST API (ASP.NET Core)
- `Front-end/Pivot` – klijentska aplikacija (Angular)
- PostgreSQL baza podataka

## Funkcionalnosti

**Korisnici i autentifikacija**
- Registracija sa aktivacijom naloga putem email linka
- Prijava (JWT), zaboravljena i resetovanje lozinke putem emaila
- Izmena profila i promena lozinke

**Table, liste i kartice**
- Kreiranje, izmena, pretraga, arhiviranje i brisanje tabli
- Saradnici na tabli sa ulogama *vlasnik* i *saradnik* (dodavanje i uklanjanje)
- Liste i kartice, premeštanje i promena redosleda
- Prikvačivanje (pin) lista i kartica
- Dodela više korisnika jednoj kartici
- Komentari na karticama (brisati se mogu samo sopstveni)
- Prilaganje i uklanjanje slika na karticama
- Istorija izmena kartice

**Aktivnosti i obaveštenja**
- Zapisnik aktivnosti na tabli (kreiranje, izmena, brisanje, premeštanje, dodela, saradnici, prilozi, ...)
- Obaveštenja u realnom vremenu preko **SignalR**-a: dodavanje na tablu, uklanjanje sa table, dodela kartice,
  novi komentar, arhiviranje i brisanje table

## Tehnologije

**Backend**
- .NET 8, ASP.NET Core Web API
- Entity Framework Core sa PostgreSQL-om (Npgsql)
- JWT autentifikacija, SignalR
- AutoMapper, FluentResults, MailKit (slanje emailova), Swagger

**Frontend**
- Angular 20, Angular Material
- `@microsoft/signalr` za obaveštenja u realnom vremenu

**Infrastruktura**
- Docker i Docker Compose (PostgreSQL 16, backend, frontend servisiran preko Nginx-a)

## Arhitektura

Backend je slojevit: `Controller` → `Service` → `Repository` → `Data` (EF Core `DbContext`), uz DTO objekte i
AutoMapper za mapiranje. Greške se u servisnom sloju vraćaju kao `Result` (FluentResults). Pristup svim
resursima tabli proverava se po korisniku (samo članovi table imaju pristup).
