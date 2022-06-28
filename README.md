# kafka-dotnet-examples

This repository is a playground for dotnet services using kafka and influx db

## Goal
The goal is to create batch kafka consumer in dotnet.
Since there is no available library that provides batch consuming in dotnet (at least unable to find it) we should take a look at what is a problem with architecture and why is there a need for batch consuming.

## Bottleneck

The API provides one message at a time, but this is from an internal queue on the client, and behind the scenes, there is a lot going on to ensure high throughput from the brokers. The client will very easily handle 50Gb/day (this is a small amount of data in Kafka terms).

The real bottleneck is on the consumer side, usually slow DB operations.

## Solution

Create a buffer on client side and perform batch processing on slower operations.