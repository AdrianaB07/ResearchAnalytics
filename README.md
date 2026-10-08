# ResearchAnalytics

**ResearchAnalytics** is a web application for collecting, consolidating, and analyzing academic research data from multiple external sources. The platform allows users to search for researchers and publications, inspect academic profiles, analyze publication and citation activity, enrich publication metadata using Scopus, and generate customizable Excel reports.

## Features

* **Researcher Search**

  * Search researchers by name.
  * Retrieve researcher profiles from OpenAlex and Semantic Scholar.
  * Access ORCID profiles using an ORCID iD.
  * View publications and citation information from multiple sources.

* **Publication Search**

  * Search for publications by title using Scopus.
  * Display bibliographic metadata and citation information.

* **Scopus Enrichment**

  * Enrich publications retrieved from OpenAlex, Semantic Scholar, or ORCID with additional Scopus metadata.
  * Match publications in Scopus based on their title.
  * Retrieve information such as authors, publication venue, DOI, citation count, and Scopus link.

* **Researcher Activity Analysis**

  * Total number of publications and citations.
  * Average citations per publication.
  * Number of active research years.
  * Standard deviation of citation counts.
  * Publication and citation trends by year.
  * Productivity and citation impact Trend Scores.

* **Customizable Excel Reports**

  * Export researcher and publication data to Excel.
  * Select the information to be included in the generated report.
  * Reuse the exported data for further analysis.

* **Authentication and Authorization**

  * User registration and login.
  * Role-based access control.
  * Separate functionality for regular users and administrators.

* **Administrative Dashboard**

  * Overview of application usage.
  * Administrative statistics and stored search information.

## Data Sources

ResearchAnalytics integrates the following academic platforms:

| Source                                               | Purpose                                         |
| ---------------------------------------------------- | ----------------------------------------------- |
| [OpenAlex](https://openalex.org/)                    | Researcher and publication discovery            |
| [Semantic Scholar](https://www.semanticscholar.org/) | Publications, authors and citation data         |
| [ORCID](https://orcid.org/)                          | Researcher identity and works                   |
| [Scopus](https://www.scopus.com/)                    | Bibliographic metadata and citation information |

## Trend Score

ResearchAnalytics uses a custom **Trend Score** to estimate the direction of a researcher's productivity and citation impact over time.

The score is based on **Cosine Similarity** between:

* the actual yearly values of publications or citations; and
* an ideal monotonically increasing reference vector.

A score closer to **1** indicates that the observed evolution more closely follows an increasing trend.

The indicator is intended as a lightweight descriptive measure and should be interpreted together with the other statistics rather than as a formal statistical test.

## Application Architecture

The application follows a layered **ASP.NET Core MVC** architecture.

* **Presentation layer** – Razor Views, HTML, CSS, Bootstrap and JavaScript.
* **Application logic** – MVC controllers and application services.
* **External API layer** – dedicated services for OpenAlex, Semantic Scholar, ORCID and Scopus.
* **Data access layer** – Entity Framework Core.
* **Database** – Microsoft SQL Server.
* **Authentication** – ASP.NET Identity.

The service-based organization of the external API integrations allows individual data sources to be maintained independently and makes it easier to extend the application with additional sources.

## Technologies

### Backend

* ASP.NET Core MVC
* C#
* Entity Framework Core
* ASP.NET Identity
* SQL Server

### Frontend

* Razor Views
* HTML5
* CSS3
* Bootstrap
* JavaScript

### APIs

* OpenAlex API
* Semantic Scholar API
* ORCID API
* Elsevier Scopus API

### Development Tools

* Visual Studio
* SQL Server Management Studio

## Limitations

The availability and completeness of the displayed information depend on the external academic sources and their APIs.

Some platforms were analyzed but could not be integrated into the final version because of API availability or access restrictions. Google Scholar does not provide an official public API, Web of Science requires appropriate institutional access, and IEEE Xplore API access could not be obtained during development. DBLP was also considered, but its available metadata does not provide the same bibliometric information required by the application.

Scopus does not provide direct author search through the API access available to the project. Therefore, the application uses the **Scopus Enrichment** approach to search individual publications by title and retrieve additional metadata.

## Future Development

Potential improvements include:

* integration of additional academic databases;
* additional bibliometric indicators such as the **h-index** and **i10-index**;
* comparison and longitudinal tracking of researchers;
* improvements to research trend analysis;
* AI-assisted researcher and publication recommendations;
* automated identification of research trends.

## Academic Project

ResearchAnalytics was developed as a master's dissertation project in **Software Engineering**.

The project focuses on the integration of heterogeneous academic data sources, research activity analysis, bibliometric visualization, and the development of a unified interface for academic information retrieval.
