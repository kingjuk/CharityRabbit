# CharityRabbit
CharityRabbit is an open source platfrom for posting and finding Good works to do around you

hosted at [CharityRabbit](https://charityrabbit.com/)

## GoodWorks Graph Schema

This document provides an overview of the Neo4j graph database schema for the GoodWorks application. It defines each node type and its properties, as well as the relationships between them.

### Schema Overview
The GoodWorks graph database consists of the following nodes and relationships:

```plaintext
(:GoodWork)-[:HAS_CONTACT]->(:Contact)
(:GoodWork)-[:BELONGS_TO]->(:Category)
(:GoodWork)-[:REQUIRES_SKILL]->(:Skill)
(:GoodWork)-[:LOCATED_IN]->(:Location)
(:GoodWork)-[:VOLUNTEERS_FOR]->(:Person)
(:GoodWork)-[:TAGGED_AS]->(:Tag)
```

### Node Definitions

#### `GoodWork`
Represents a volunteer opportunity or charitable project.

**Properties:**
- `name` (String): The name of the good work.
- `description` (String): A detailed description of the good work.
- `latitude` (Float): Geographic latitude coordinate.
- `longitude` (Float): Geographic longitude coordinate.
- `startTime` (DateTime): The planned start time of the project.
- `endTime` (DateTime): The planned end time of the project.
- `effortLevel` (String): Effort level required (e.g., Light, Moderate, Heavy).
- `isAccessible` (Boolean): Whether the project is accessible.
- `isVirtual` (Boolean): Indicates if the project is virtual.
- `estimatedDuration` (Integer): Estimated duration in minutes.

#### `Contact`
Represents the contact information for a good work.

**Properties:**
- `name` (String): Contact person's name.
- `email` (String): Contact person's email.
- `phone` (String): Contact person's phone number.

#### `Category`
Represents the type of work the project belongs to.

**Properties:**
- `name` (String): The category name (e.g., Environmental, Education, Health).
- `description` (String): A brief description of the category.

#### `Skill`
Represents a skill required to participate in a good work.

**Properties:**
- `name` (String): The name of the skill (e.g., Carpentry, Teaching, First Aid).
- `description` (String): A brief description of the skill.

#### `Location`
Represents the geographical location of a good work.

**Properties:**
- `city` (String): City where the work is located.
- `state` (String): State where the work is located.
- `country` (String): Country where the work is located.
- `zip` (String): Zip code of the location.

#### `Person`
Represents a volunteer who is participating in a good work.

**Properties:**
- `name` (String): Full name of the person.
- `email` (String): Email address of the person.
- `phone` (String): Phone number of the person.
- `skills` (List of Strings): Skills possessed by the person.

#### `Tag`
Represents a tag associated with a good work.

**Properties:**
- `name` (String): The tag name (e.g., Community, Family-friendly).

### Relationship Definitions

#### `HAS_CONTACT`
- **Description:** Links a `GoodWork` to a `Contact`.
- **Direction:** `(:GoodWork)-[:HAS_CONTACT]->(:Contact)`

#### `BELONGS_TO`
- **Description:** Links a `GoodWork` to a `Category`.
- **Direction:** `(:GoodWork)-[:BELONGS_TO]->(:Category)`

#### `REQUIRES_SKILL`
- **Description:** Links a `GoodWork` to required `Skill` nodes.
- **Direction:** `(:GoodWork)-[:REQUIRES_SKILL]->(:Skill)`

#### `LOCATED_IN`
- **Description:** Links a `GoodWork` to a `Location`.
- **Direction:** `(:GoodWork)-[:LOCATED_IN]->(:Location)`

#### `VOLUNTEERS_FOR`
- **Description:** Links a `Person` to a `GoodWork` they are volunteering for.
- **Direction:** `(:Person)-[:VOLUNTEERS_FOR]->(:GoodWork)`

#### `TAGGED_AS`
- **Description:** Links a `GoodWork` to associated `Tag` nodes.
- **Direction:** `(:GoodWork)-[:TAGGED_AS]->(:Tag)`

### Example Queries

#### Find all good works in a specific category:
```cypher
MATCH (g:GoodWork)-[:BELONGS_TO]->(c:Category {name: 'Environmental'})
RETURN g
```

#### Find all good works in a specific location:
```cypher
MATCH (g:GoodWork)-[:LOCATED_IN]->(l:Location {city: 'New York'})
RETURN g
```

#### Find all skills required for a good work:
```cypher
MATCH (g:GoodWork)-[:REQUIRES_SKILL]->(s:Skill)
WHERE g.name = 'Park Cleanup'
RETURN s
```

#### Find all volunteers for a good work:
```cypher
MATCH (p:Person)-[:VOLUNTEERS_FOR]->(g:GoodWork {name: 'Park Cleanup'})
RETURN p
```

#### Find all good works in a specific zip code:
```cypher
MATCH (g:GoodWork)-[:LOCATED_IN]->(l:Location {zip: '10001'})
RETURN g
```

