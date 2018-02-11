using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Vega;

//Entity Class
[Table(NeedsHistory = true)]
public class Country : EntityBase
{
    [PrimaryKey(true)]
    public int Id { get; set; }
    public string Name { get; set; }
    public string ShortCode { get; set; }
}

//Insert, Update, Delete & Read Operation Class
public class CountryRepo
{
    string conString = "Data Source=.;Initial Catalog=tempdb;Integrated Security=True";
    Session currentSession = new Session(1);

    public int Add(Country country)
    {
        using (SqlConnection connection = new SqlConnection(conString))
        {
            Repository<Country> countryRepo = new Repository<Country>(connection, currentSession);
            return (int)countryRepo.Add(country);
        }
    }

    public bool Update(Country country)
    {
        using (SqlConnection connection = new SqlConnection(conString))
        {
            Repository<Country> countryRepo = new Repository<Country>(connection, currentSession);
            return countryRepo.Update(country);
        }
    }

    public bool Delete(int countryId)
    {
        using (SqlConnection connection = new SqlConnection(conString))
        {
            Repository<Country> countryRepo = new Repository<Country>(connection, currentSession);
            return countryRepo.Delete(countryId);
        }
    }

    public Country ReadOne(int countryId)
    {
        using (SqlConnection connection = new SqlConnection(conString))
        {
            Repository<Country> countryRepo = new Repository<Country>(connection, currentSession);
            return countryRepo.ReadOne(countryId);
        }
    }

    public List<Country> ReadAll()
    {
        using (SqlConnection connection = new SqlConnection(conString))
        {
            Repository<Country> countryRepo = new Repository<Country>(connection, currentSession);
            return countryRepo.ReadAll().ToList();
        }
    }
}

public class Demo
{
    public void Do()
    {
        
        CountryRepo cr = new CountryRepo();

        //Add
        Country country = new Country()
        {
            Name = "India",
            ShortCode = "IN"
        };
        country.Id = cr.Add(country);

        //update
        country.ShortCode = "IND";
        cr.Update(country);

        //read one
        country = cr.ReadOne(country.Id);

        //read all
        List<Country> lstCountry = cr.ReadAll();

        //delete
        cr.Delete(country.Id);
    }
}
