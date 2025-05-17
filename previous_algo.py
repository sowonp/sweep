import pandas as pd
import random
import heapq
import numpy as np
import time
import os
import glob

# ================================================
# 작업 디렉토리 설정
# ================================================
os.chdir("C:/")
os.makedirs("algo_test_results", exist_ok=True)

# ================================================
# CSV 데이터 로드
# ================================================
def load_data(file_path):
    return pd.read_csv(file_path)

data = load_data('2024.csv')

# ================================================
# 환경 기반 쓰레기 및 장애물 생성 함수
# ================================================
def generate_trash_obstacles_with_environment(grid_size, env_row):
    # === 환경 데이터 읽기 ===
    wind_speed = env_row['풍속(m/s)']
    wind_direction = env_row['풍향(deg)']
    gust_speed = env_row['GUST풍속(m/s)']
    max_wave_height = env_row['최대파고(m)']
    significant_wave_height = env_row['유의파고(m)']
    mean_wave_height = env_row['평균파고(m)']
    wave_period = env_row['파주기(sec)']
    wave_direction = env_row['파향(deg)']

    # === NaN 처리 (기본값 설정) ===
    if pd.isna(wind_speed):
        wind_speed = 0
    if pd.isna(wind_direction):
        wind_direction = 0
    if pd.isna(gust_speed):
        gust_speed = 0
    if pd.isna(max_wave_height):
        max_wave_height = 0
    if pd.isna(significant_wave_height):
        significant_wave_height = 0
    if pd.isna(mean_wave_height):
        mean_wave_height = 0
    if pd.isna(wave_period):
        wave_period = 0
    if pd.isna(wave_direction):
        wave_direction = 0

    # === 쓰레기와 장애물 개수 결정 ===
    base_trash = 50
    base_obstacles = 10

    trash_count = base_trash + int(wind_speed * 5) + int(significant_wave_height * 10)
    obstacle_count = base_obstacles + int(gust_speed * 2) + int(max_wave_height * 5)

    trash_count = min(trash_count, 300)
    obstacle_count = min(obstacle_count, 50)

    # === 쓰레기 배치 (바람 방향 고려) ===
    trash_positions = []
    for _ in range(trash_count):
        base_x = random.randint(0, grid_size-1)
        base_y = random.randint(0, grid_size-1)
        
        offset_distance = random.randint(0, 5)
        offset_x = int(np.cos(np.radians(wind_direction)) * offset_distance)
        offset_y = int(np.sin(np.radians(wind_direction)) * offset_distance)

        new_x = max(0, min(grid_size-1, base_x + offset_x))
        new_y = max(0, min(grid_size-1, base_y + offset_y))
        
        trash_positions.append((new_x, new_y))

    # === 장애물 배치 (파향 고려) ===
    obstacle_positions = []
    for _ in range(obstacle_count):
        base_x = random.randint(0, grid_size-1)
        base_y = random.randint(0, grid_size-1)
        
        offset_distance = random.randint(0, 3)
        offset_x = int(np.cos(np.radians(wave_direction)) * offset_distance)
        offset_y = int(np.sin(np.radians(wave_direction)) * offset_distance)

        new_x = max(0, min(grid_size-1, base_x + offset_x))
        new_y = max(0, min(grid_size-1, base_y + offset_y))

        obstacle_positions.append((new_x, new_y))

    return trash_positions, obstacle_positions

# ================================================
# 탐욕 알고리즘
# ================================================
def greedy_algorithm(start, trash_positions, obstacle_positions):
    path = [start]
    current_position = start

    while trash_positions:
        next_position = min(trash_positions, key=lambda p: np.linalg.norm(np.array(p) - np.array(current_position)))
        path.append(next_position)
        trash_positions.remove(next_position)
        current_position = next_position
    
    return path

# ================================================
# 다익스트라 알고리즘
# ================================================
def dijkstra_algorithm(start, trash_positions, obstacle_positions):
    path = []
    grid_size = 50
    directions = [(-1, 0), (1, 0), (0, -1), (0, 1)]
    visited = set()
    pq = [(0, start, [])]

    while pq and trash_positions:
        cost, current_position, current_path = heapq.heappop(pq)

        if current_position in visited:
            continue

        visited.add(current_position)
        current_path = current_path + [current_position]

        if current_position in trash_positions:
            trash_positions.remove(current_position)
            path.extend(current_path)
            current_path = []

        for d in directions:
            neighbor = (current_position[0] + d[0], current_position[1] + d[1])
            if (0 <= neighbor[0] < grid_size and 0 <= neighbor[1] < grid_size and
                neighbor not in visited and neighbor not in obstacle_positions):
                heapq.heappush(pq, (cost + 1, neighbor, current_path))
    
    return path

# ================================================
# A* 알고리즘
# ================================================
def a_star_algorithm(start, trash_positions, obstacle_positions):
    path = []
    grid_size = 50
    directions = [(-1, 0), (1, 0), (0, -1), (0, 1)]
    open_list = [(0, start, [])]
    closed_set = set()

    while open_list and trash_positions:
        _, current_position, current_path = min(open_list, key=lambda p: p[0])
        open_list.remove((_, current_position, current_path))

        if current_position in closed_set:
            continue

        closed_set.add(current_position)
        current_path = current_path + [current_position]

        if current_position in trash_positions:
            trash_positions.remove(current_position)
            path.extend(current_path)
            current_path = []

        for d in directions:
            neighbor = (current_position[0] + d[0], current_position[1] + d[1])
            if (0 <= neighbor[0] < grid_size and 0 <= neighbor[1] < grid_size and
                neighbor not in closed_set and neighbor not in obstacle_positions):
                cost = len(current_path) + np.linalg.norm(np.array(neighbor) - np.array(current_position))
                open_list.append((cost, neighbor, current_path))
    
    return path

# ================================================
# D* 알고리즘
# ================================================
def d_star_algorithm(start, trash_positions, obstacle_positions):
    grid_size = 50
    directions = [(-1, 0), (1, 0), (0, -1), (0, 1)]
    path = []
    current_position = start

    def heuristic(a, b):
        return np.linalg.norm(np.array(a) - np.array(b))

    def a_star(start, goal, obstacles):
        open_set = []
        heapq.heappush(open_set, (0 + heuristic(start, goal), 0, start, [start]))
        visited = set()

        while open_set:
            est_total_cost, cost_so_far, current, current_path = heapq.heappop(open_set)

            if current == goal:
                return current_path

            if current in visited:
                continue

            visited.add(current)

            for d in directions:
                neighbor = (current[0] + d[0], current[1] + d[1])
                if (0 <= neighbor[0] < grid_size and 0 <= neighbor[1] < grid_size and neighbor not in obstacles):
                    if neighbor not in visited:
                        new_cost = cost_so_far + 1
                        est_total = new_cost + heuristic(neighbor, goal)
                        heapq.heappush(open_set, (est_total, new_cost, neighbor, current_path + [neighbor]))

        return None

    while trash_positions:
        next_target = min(trash_positions, key=lambda p: heuristic(current_position, p))
        route = a_star(current_position, next_target, set(obstacle_positions))

        if route is None:
            trash_positions.remove(next_target)
            continue

        path.extend(route[1:])
        current_position = next_target
        trash_positions.remove(next_target)

    return path

# ================================================
# 알고리즘 성능 평가 함수
# ================================================
def evaluate_algorithm(algorithm, start, trash_positions, obstacle_positions):
    start_time = time.time()
    results = algorithm(start, trash_positions.copy(), obstacle_positions)
    end_time = time.time()
    
    collection_rate = len(set(results) & set(trash_positions)) / len(trash_positions) if trash_positions else 0
    collision_count = len([pos for pos in results if pos in obstacle_positions])
    search_distance = len(results)
    elapsed_time = end_time - start_time
    
    return {
        "collection_rate": collection_rate,
        "collision_count": collision_count,
        "search_distance": search_distance,
        "elapsed_time": elapsed_time
    }

# ================================================
# 결과 출력 함수
# ================================================
def print_results(results):
    for name, metrics in results.items():
        print(f"{name} Algorithm:")
        print(f"  수집률: {metrics['collection_rate']:.4f}")
        print(f"  충돌 횟수: {metrics['collision_count']}")
        print(f"  탐색 거리: {metrics['search_distance']}")
        print(f"  실행 시간: {metrics['elapsed_time']:.4f} seconds")
        print()

# ================================================
# 결과 저장 함수
# ================================================
save_folder = "algo_test_results"
os.makedirs(save_folder, exist_ok=True)

algorithm_names = ["❤️Greedy", "💛Dijkstra", "💚A-Star", "💙D-Star"]

def save_partial_results(all_results, save_count):
    rows = []
    for i, result in enumerate(all_results):
        for algo in algorithm_names:
            row = {
                "Index": i,
                "Algorithm": algo,
                "CollectionRate": result[algo]["collection_rate"],
                "CollisionCount": result[algo]["collision_count"],
                "SearchDistance": result[algo]["search_distance"],
                "ElapsedTime": result[algo]["elapsed_time"]
            }
            rows.append(row)
    df = pd.DataFrame(rows)
    df.to_csv(f"{save_folder}/algorithm_test_results_{save_count}.csv", index=False, encoding='utf-8-sig')

# ================================================
# 메인 실행
# ================================================
all_results = []
batch_size = 500
save_count = 1
total = len(data)

for index, row in data.iterrows():
    trash_positions, obstacle_positions = generate_trash_obstacles_with_environment(50, row)
    start = (0, 0)

    greedy_results = evaluate_algorithm(greedy_algorithm, start, trash_positions, obstacle_positions)
    dijkstra_results = evaluate_algorithm(dijkstra_algorithm, start, trash_positions, obstacle_positions)
    a_star_results = evaluate_algorithm(a_star_algorithm, start, trash_positions, obstacle_positions)
    d_star_results = evaluate_algorithm(d_star_algorithm, start, trash_positions, obstacle_positions)

    results = {
        "❤️Greedy": greedy_results,
        "💛Dijkstra": dijkstra_results,
        "💚A-Star": a_star_results,
        "💙D-Star": d_star_results
    }
    
    all_results.append(results)
    
    print()
    print("================================================")
    print(f"Data Index {index+1}/{total}: ({(index+1)/total*100:.2f}%)")
    print()
    print_results(results)

    if (index + 1) % batch_size == 0:
        save_partial_results(all_results, save_count)
        print(f"\n✅ {batch_size}개 저장 완료: algorithm_test_results_{save_count}.csv\n")
        all_results.clear()
        save_count += 1

if all_results:
    save_partial_results(all_results, save_count)
    print("\n================================================")
    print(f"✅ TEST RESULT FILE SAVED: algorithm_test_results_{save_count}.csv")
    print("================================================")
    print()

# ================================================
# 평균 결과 계산
# ================================================
print("\n================================================")
print("✅ TEST RESULT AVERAGE")
file_list = glob.glob(f"{save_folder}/algorithm_test_results_*.csv")

dfs = []
for file in file_list:
    dfs.append(pd.read_csv(file))

full_df = pd.concat(dfs, ignore_index=True)

for algo in algorithm_names:
    algo_df = full_df[full_df["Algorithm"] == algo]
    print(f"\n=== {algo} Algorithm ===")
    print(f"  수집률 평균: {algo_df['CollectionRate'].mean():.4f}")
    print(f"  충돌 횟수 평균: {algo_df['CollisionCount'].mean():.4f}")
    print(f"  탐색 거리 평균: {algo_df['SearchDistance'].mean():.4f}")
    print(f"  실행 시간 평균: {algo_df['ElapsedTime'].mean():.4f} seconds")

print()