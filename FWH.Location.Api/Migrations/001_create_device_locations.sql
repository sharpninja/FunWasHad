-- Migration: Add device_locations table
-- Date: 2025-01-08
-- Description: Creates table for tracking device locations

CREATE TABLE IF NOT EXISTS device_locations (
    id SERIAL PRIMARY KEY,
    device_id VARCHAR(100) NOT NULL,
    latitude DOUBLE PRECISION NOT NULL,
    longitude DOUBLE PRECISION NOT NULL,
    accuracy_meters DOUBLE PRECISION,
    timestamp TIMESTAMPTZ NOT NULL,
    recorded_at TIMESTAMPTZ NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    
    CONSTRAINT device_locations_latitude_check CHECK (latitude >= -90 AND latitude <= 90),
    CONSTRAINT device_locations_longitude_check CHECK (longitude >= -180 AND longitude <= 180),
    CONSTRAINT device_locations_accuracy_check CHECK (accuracy_meters IS NULL OR accuracy_meters >= 0)
);

-- Create indexes for efficient querying
CREATE INDEX IF NOT EXISTS idx_device_locations_device_id ON device_locations(device_id);
CREATE INDEX IF NOT EXISTS idx_device_locations_timestamp ON device_locations(timestamp);
CREATE INDEX IF NOT EXISTS idx_device_locations_recorded_at ON device_locations(recorded_at);

-- Optional: Create composite index for device + timestamp queries
CREATE INDEX IF NOT EXISTS idx_device_locations_device_timestamp ON device_locations(device_id, timestamp DESC);

-- Add comments for documentation
COMMENT ON TABLE device_locations IS 'Stores device location updates for tracking movement';
COMMENT ON COLUMN device_locations.device_id IS 'Unique identifier for the device';
COMMENT ON COLUMN device_locations.latitude IS 'GPS latitude in decimal degrees (-90 to 90)';
COMMENT ON COLUMN device_locations.longitude IS 'GPS longitude in decimal degrees (-180 to 180)';
COMMENT ON COLUMN device_locations.accuracy_meters IS 'GPS accuracy radius in meters';
COMMENT ON COLUMN device_locations.timestamp IS 'When the location was captured on the device';
COMMENT ON COLUMN device_locations.recorded_at IS 'When the location was recorded in the database';
