-- PostGIS spatial index migration
-- This migration is optional and will be skipped if PostGIS extension is not available
-- (e.g., in test environments where PostGIS is not installed)

DO $$
BEGIN
    -- Try to enable PostGIS extension for spatial queries
    -- If PostGIS is not available, this will fail gracefully and skip the rest
    BEGIN
        CREATE EXTENSION IF NOT EXISTS postgis;

        -- Add geometry column to businesses table for spatial indexing
        -- This column stores the business location as a Point geometry
        ALTER TABLE businesses
        ADD COLUMN IF NOT EXISTS location_geometry geometry(Point, 4326);

        -- Create spatial GIST index for efficient spatial queries
        -- GIST indexes are optimized for geometric data types
        CREATE INDEX IF NOT EXISTS idx_businesses_location_geometry
        ON businesses USING GIST (location_geometry);

        -- Populate the geometry column from existing latitude/longitude data
        -- ST_SetSRID sets the spatial reference system (4326 = WGS84)
        -- ST_MakePoint creates a Point geometry from longitude, latitude
        UPDATE businesses
        SET location_geometry = ST_SetSRID(ST_MakePoint(longitude, latitude), 4326)
        WHERE location_geometry IS NULL
          AND latitude IS NOT NULL
          AND longitude IS NOT NULL;

        -- Create a trigger function to automatically update geometry when lat/lon changes
        CREATE OR REPLACE FUNCTION update_business_location_geometry()
        RETURNS TRIGGER AS $trigger$
        BEGIN
            IF NEW.latitude IS NOT NULL AND NEW.longitude IS NOT NULL THEN
                NEW.location_geometry := ST_SetSRID(ST_MakePoint(NEW.longitude, NEW.latitude), 4326);
            ELSE
                NEW.location_geometry := NULL;
            END IF;
            RETURN NEW;
        END;
        $trigger$ LANGUAGE plpgsql;

        -- Create trigger to automatically update geometry on insert or update
        DROP TRIGGER IF EXISTS trigger_update_business_location_geometry ON businesses;
        CREATE TRIGGER trigger_update_business_location_geometry
            BEFORE INSERT OR UPDATE OF latitude, longitude ON businesses
            FOR EACH ROW
            EXECUTE FUNCTION update_business_location_geometry();

        -- Add comment for documentation
        COMMENT ON COLUMN businesses.location_geometry IS 'Spatial geometry column for efficient PostGIS spatial queries. Automatically maintained from latitude/longitude columns.';
    EXCEPTION
        WHEN OTHERS THEN
            RAISE NOTICE 'PostGIS extension is not available (error: %). Skipping PostGIS spatial index migration.', SQLERRM;
    END;
END $$;
